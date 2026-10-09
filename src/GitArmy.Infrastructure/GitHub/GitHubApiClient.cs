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

            var repos = await _httpClient.GetFromJsonAsync<List<GitHubRepoResponse>>(
                $"users/{username}/repos?per_page=100&type=owner", cancellationToken) ?? [];

            var languages = repos
                .Where(r => !r.Fork && r.Language is not null)
                .Select(r => r.Language!)
                .Distinct()
                .ToList();

            var originalRepos = repos.Count(r => !r.Fork);
            var totalStars    = repos.Where(r => !r.Fork).Sum(r => r.StargazersCount);

            var activity = await GetActivityAsync(username, user.CreatedAt.Year, cancellationToken);

            var (prsCreated, prsMerged) = await GetPRCountsAsync(username, cancellationToken);

            return new GitHubProfileData(
                Username:            user.Login,
                PublicRepos:         originalRepos,
                Followers:           user.Followers,
                CreatedAt:           user.CreatedAt,
                Languages:           languages,
                TotalCommitsLastYear: activity.Commits,
                ActiveWeeksLastYear: activity.ActiveWeeks,
                YearlyContributions: activity.YearlyContributions,
                TotalPRsCreated:     prsCreated,
                TotalPRsMerged:      prsMerged,
                TotalStars:          totalStars);
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

    // Uma única consulta GraphQL fornece as métricas de atividade:
    //  - commits do último ano;
    //  - semanas do último ano com pelo menos 1 contribuição (consistência);
    //  - total de contribuições de cada ano civil desde a criação da conta (um alias por ano).
    // `contributionYears` não serve para isto: lista todos os anos desde a criação da conta, haja ou não atividade.
    // Não há fallback: os eventos REST só cobrem ~90 dias / 300 eventos e davam pontuações diferentes
    // consoante o token, por isso uma falha aqui é reportada em vez de pontuar com dados incompletos.
    private async Task<(int Commits, int ActiveWeeks, IReadOnlyList<int> YearlyContributions)> GetActivityAsync(
        string username, int firstYear, CancellationToken cancellationToken)
    {
        var lastYear = DateTime.UtcNow.Year;
        var payload = JsonSerializer.Serialize(new
        {
            query = BuildActivityQuery(firstYear, lastYear),
            variables = new { login = username },
        });
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
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new GitHubUnavailableException(
                    $"O GitHub recusou a consulta de atividade (HTTP {(int)response.StatusCode}). Verifique o token em GitHub:Token e o limite de pedidos.");

            if (!response.IsSuccessStatusCode)
                throw new GitHubUnavailableException(
                    $"O GitHub respondeu HTTP {(int)response.StatusCode} à consulta de atividade do perfil.");

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            if (root.TryGetProperty("errors", out var errors))
                throw new GitHubUnavailableException(
                    $"O GitHub devolveu erros na consulta de atividade: {FirstErrorMessage(errors)}");

            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object ||
                !data.TryGetProperty("user", out var user) || user.ValueKind != JsonValueKind.Object)
                throw new GitHubUnavailableException("O GitHub não devolveu dados de atividade para este perfil.");

            var collection = user.GetProperty("contributionsCollection");

            var commits = collection.GetProperty("totalCommitContributions").GetInt32();
            var weeks   = collection.GetProperty("contributionCalendar").GetProperty("weeks").EnumerateArray()
                .Count(w => w.GetProperty("contributionDays").EnumerateArray()
                    .Any(d => d.GetProperty("contributionCount").GetInt32() > 0));

            var yearly = new List<int>();
            for (var year = firstYear; year <= lastYear; year++)
                yearly.Add(user.GetProperty($"y{year}").GetProperty("contributionCalendar")
                    .GetProperty("totalContributions").GetInt32());

            _logger.LogDebug("Contributions per year for {Username}: {Years}", username,
                string.Join(", ", yearly.Select((c, i) => $"{firstYear + i}={c}")));

            return (commits, weeks, yearly);
        }
    }

    // Os anos são inteiros gerados aqui (não vêm do utilizador), por isso podem ir em linha na consulta.
    private static string BuildActivityQuery(int firstYear, int lastYear)
    {
        var query = new StringBuilder(
            "query($login: String!) { user(login: $login) { " +
            "contributionsCollection { totalCommitContributions " +
            "contributionCalendar { weeks { contributionDays { contributionCount } } } }");

        for (var year = firstYear; year <= lastYear; year++)
            query.Append($" y{year}: contributionsCollection(from: \"{year}-01-01T00:00:00Z\", to: \"{year}-12-31T23:59:59Z\") " +
                         "{ contributionCalendar { totalContributions } }");

        return query.Append(" } }").ToString();
    }

    private static string FirstErrorMessage(JsonElement errors) =>
        errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0 &&
        errors[0].TryGetProperty("message", out var message)
            ? message.GetString() ?? "erro desconhecido"
            : "erro desconhecido";
}
