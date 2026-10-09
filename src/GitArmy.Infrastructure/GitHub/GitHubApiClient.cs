using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GitArmy.Application.Interfaces;
using GitArmy.Infrastructure.GitHub.Models;
using Microsoft.Extensions.Logging;

namespace GitArmy.Infrastructure.GitHub;

internal sealed class GitHubApiClient : IGitHubClient
{
    // Repositórios próprios e não-fork, dos mais para os menos estrelados (até 100). A ordem por estrelas
    // garante que os repos relevantes entram mesmo em contas com mais de 100 repositórios; a listagem REST
    // era alfabética e cortava os restantes. `ownerAffiliations: OWNER` exclui os repos de organizações.
    private const string RepositoriesQuery = """
        query($login: String!) {
          user(login: $login) {
            repositories(first: 100, ownerAffiliations: OWNER, isFork: false, orderBy: {field: STARGAZERS, direction: DESC}) {
              nodes {
                stargazerCount
                isEmpty
                languages(first: 6, orderBy: {field: SIZE, direction: DESC}) { edges { size node { name } } }
              }
            }
          }
        }
        """;

    private const string RecentActivityQuery = """
        query($login: String!) {
          user(login: $login) {
            contributionsCollection {
              totalCommitContributions
              contributionCalendar { weeks { contributionDays { contributionCount } } }
            }
          }
        }
        """;

    // O total de cada ano é pedido em blocos pequenos: uma única consulta com todos os anos excedia os limites
    // de recursos do GitHub em contas com muitos anos de atividade intensa.
    private const int YearsPerRequest = 4;
    private const int MaxParallelRequests = 3;

    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubApiClient> _logger;

    public GitHubApiClient(HttpClient httpClient, ILogger<GitHubApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GitHubProfileData?> GetProfileAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _httpClient.GetFromJsonAsync<GitHubUserResponse>(
                $"users/{username}", cancellationToken);

            if (user is null)
                return null;

            // As consultas são independentes: correm em paralelo.
            var recentTask = GetRecentActivityAsync(username, cancellationToken);
            var yearlyTask = GetYearlyContributionsAsync(username, user.CreatedAt.Year, cancellationToken);
            var reposTask  = GetRepositoriesAsync(username, cancellationToken);
            var prsTask    = GetPRCountsAsync(username, cancellationToken);
            await Task.WhenAll(recentTask, yearlyTask, reposTask, prsTask);

            var recent = await recentTask;
            var (prsCreated, prsMerged) = await prsTask;

            return new GitHubProfileData(
                Username:            user.Login,
                Followers:           user.Followers,
                CreatedAt:           user.CreatedAt,
                Repos:               await reposTask,
                TotalCommitsLastYear: recent.Commits,
                ActiveWeeksLastYear: recent.ActiveWeeks,
                YearlyContributions: await yearlyTask,
                TotalPRsCreated:     prsCreated,
                TotalPRsMerged:      prsMerged);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _logger.LogError(ex, "GitHub rejected the request for {Username}", username);
            throw new GitHubUnavailableException(
                $"O GitHub recusou o pedido (HTTP {(int)ex.StatusCode!.Value}). Verifique o token em GitHub:Token e o limite de pedidos.", ex);
        }
        catch (Exception ex) when (ex is not GitHubUnavailableException)
        {
            _logger.LogError(ex, "Failed to fetch GitHub profile for {Username}", username);
            throw;
        }
    }

    private async Task<(int Created, int Merged)> GetPRCountsAsync(string username, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _httpClient.GetFromJsonAsync<GitHubSearchResponse>(
                $"search/issues?q=author:{username}+type:pr&per_page=1", cancellationToken);

            var merged = await _httpClient.GetFromJsonAsync<GitHubSearchResponse>(
                $"search/issues?q=author:{username}+type:pr+is:merged&per_page=1", cancellationToken);

            return (created?.TotalCount ?? 0, merged?.TotalCount ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch PR counts for {Username}", username);
            return (0, 0);
        }
    }

    private Task<IReadOnlyList<GitHubRepoData>> GetRepositoriesAsync(string username, CancellationToken cancellationToken) =>
        QueryUserAsync("repositórios", RepositoriesQuery, username, user =>
        {
            var repos = new List<GitHubRepoData>();

            foreach (var node in user.GetProperty("repositories").GetProperty("nodes").EnumerateArray())
            {
                if (node.ValueKind != JsonValueKind.Object)
                    continue;

                var bytes = new Dictionary<string, long>();
                foreach (var edge in node.GetProperty("languages").GetProperty("edges").EnumerateArray())
                    bytes[edge.GetProperty("node").GetProperty("name").GetString()!] = edge.GetProperty("size").GetInt64();

                repos.Add(new GitHubRepoData(
                    Stars:         node.GetProperty("stargazerCount").GetInt32(),
                    IsEmpty:       node.GetProperty("isEmpty").GetBoolean(),
                    LanguageBytes: bytes));
            }

            return (IReadOnlyList<GitHubRepoData>)repos;
        }, cancellationToken);

    // Commits do último ano e semanas desse ano com pelo menos 1 contribuição (consistência).
    private Task<(int Commits, int ActiveWeeks)> GetRecentActivityAsync(string username, CancellationToken cancellationToken) =>
        QueryUserAsync("atividade recente", RecentActivityQuery, username, user =>
        {
            var collection = user.GetProperty("contributionsCollection");

            var commits = collection.GetProperty("totalCommitContributions").GetInt32();
            var weeks   = collection.GetProperty("contributionCalendar").GetProperty("weeks").EnumerateArray()
                .Count(w => w.GetProperty("contributionDays").EnumerateArray()
                    .Any(d => d.GetProperty("contributionCount").GetInt32() > 0));

            return (commits, weeks);
        }, cancellationToken);

    // Total de contribuições de cada ano civil desde a criação da conta (um alias por ano).
    // `contributionYears` não serve para isto: lista todos os anos desde a criação da conta, haja ou não atividade.
    // Não há fallback: os eventos REST só cobrem ~90 dias / 300 eventos e davam pontuações diferentes
    // consoante o token, por isso uma falha aqui é reportada em vez de pontuar com dados incompletos.
    private async Task<IReadOnlyList<int>> GetYearlyContributionsAsync(
        string username, int firstYear, CancellationToken cancellationToken)
    {
        var lastYear = DateTime.UtcNow.Year;

        var blocks = new List<(int From, int To)>();
        for (var from = firstYear; from <= lastYear; from += YearsPerRequest)
            blocks.Add((from, Math.Min(from + YearsPerRequest - 1, lastYear)));

        using var gate = new SemaphoreSlim(MaxParallelRequests);

        var tasks = blocks.Select(async block =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await QueryUserAsync("contribuições por ano", BuildYearsQuery(block.From, block.To), username, user =>
                    Enumerable.Range(block.From, block.To - block.From + 1)
                        .Select(year => user.GetProperty($"y{year}").GetProperty("contributionCalendar")
                            .GetProperty("totalContributions").GetInt32())
                        .ToList(), cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }).ToList();

        // Task.WhenAll devolve os resultados pela ordem das tarefas, ou seja, dos anos mais antigos para os recentes.
        var yearly = (await Task.WhenAll(tasks)).SelectMany(block => block).ToList();

        _logger.LogDebug("Contributions per year for {Username}: {Years}", username,
            string.Join(", ", yearly.Select((c, i) => $"{firstYear + i}={c}")));

        return yearly;
    }

    // Os anos são inteiros gerados aqui (não vêm do utilizador), por isso podem ir em linha na consulta.
    private static string BuildYearsQuery(int fromYear, int toYear)
    {
        var query = new StringBuilder("query($login: String!) { user(login: $login) {");

        for (var year = fromYear; year <= toYear; year++)
            query.Append($" y{year}: contributionsCollection(from: \"{year}-01-01T00:00:00Z\", to: \"{year}-12-31T23:59:59Z\") " +
                         "{ contributionCalendar { totalContributions } }");

        return query.Append(" } }").ToString();
    }

    // Executa uma consulta GraphQL sobre `user(login:)` e entrega o nó `user` a `parse`.
    // Qualquer falha (token recusado, limite, erro do GraphQL, utilizador sem dados) vira GitHubUnavailableException
    // e diz qual das consultas falhou. Os 502/503/504 (transitórios) têm uma nova tentativa.
    private async Task<T> QueryUserAsync<T>(
        string what, string query, string username, Func<JsonElement, T> parse, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new { query, variables = new { login = username } });

        for (var attempt = 1; ; attempt++)
        {
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync("graphql", content, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new GitHubUnavailableException("Não foi possível contactar o GitHub.", ex);
            }

            using (response)
            {
                if (response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
                    && attempt < 2)
                {
                    _logger.LogWarning("GitHub returned {Status} for '{What}' of {Username}; retrying", (int)response.StatusCode, what, username);
                    await Task.Delay(TimeSpan.FromMilliseconds(600), cancellationToken);
                    continue;
                }

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    throw new GitHubUnavailableException(
                        $"O GitHub recusou a consulta de {what} (HTTP {(int)response.StatusCode}). Verifique o token em GitHub:Token e o limite de pedidos.");

                if (!response.IsSuccessStatusCode)
                    throw new GitHubUnavailableException(
                        $"O GitHub respondeu HTTP {(int)response.StatusCode} à consulta de {what}.");

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = doc.RootElement;

                if (root.TryGetProperty("errors", out var errors))
                    throw new GitHubUnavailableException(
                        $"O GitHub devolveu erros na consulta de {what}: {FirstErrorMessage(errors)}");

                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object ||
                    !data.TryGetProperty("user", out var user) || user.ValueKind != JsonValueKind.Object)
                    throw new GitHubUnavailableException($"O GitHub não devolveu dados de {what} para este perfil.");

                return parse(user);
            }
        }
    }

    private static string FirstErrorMessage(JsonElement errors) =>
        errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0 &&
        errors[0].TryGetProperty("message", out var message)
            ? message.GetString() ?? "erro desconhecido"
            : "erro desconhecido";
}
