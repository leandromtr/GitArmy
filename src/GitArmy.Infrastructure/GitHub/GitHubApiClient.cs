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

            var totalCommitsLastYear = await GetCommitsLastYearAsync(username, cancellationToken);

            var (prsCreated, prsMerged) = await GetPRCountsAsync(username, cancellationToken);

            return new GitHubProfileData(
                Username:            user.Login,
                PublicRepos:         originalRepos,
                Followers:           user.Followers,
                CreatedAt:           user.CreatedAt,
                Languages:           languages,
                TotalCommitsLastYear: totalCommitsLastYear,
                TotalPRsCreated:     prsCreated,
                TotalPRsMerged:      prsMerged,
                TotalStars:          totalStars);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
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

    // Tries GraphQL first (accurate annual total; requires PAT with read:user scope).
    // Falls back to summing PushEvent commits from the REST events endpoint (3 pages, ~300 events).
    private async Task<int> GetCommitsLastYearAsync(string username, CancellationToken cancellationToken)
    {
        var graphqlCount = await TryGetCommitsViaGraphQlAsync(username, cancellationToken);
        if (graphqlCount.HasValue)
            return graphqlCount.Value;

        return await GetCommitsViaEventsAsync(username, cancellationToken);
    }

    private async Task<int?> TryGetCommitsViaGraphQlAsync(string username, CancellationToken cancellationToken)
    {
        try
        {
            var query = $$"""{"query":"{ user(login: \"{{username}}\") { contributionsCollection { totalCommitContributions } } }"}""";

            using var content = new StringContent(query, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.github.com/graphql", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var root = doc.RootElement;
            if (root.TryGetProperty("errors", out _))
                return null;

            return root
                .GetProperty("data")
                .GetProperty("user")
                .GetProperty("contributionsCollection")
                .GetProperty("totalCommitContributions")
                .GetInt32();
        }
        catch
        {
            return null;
        }
    }

    private async Task<int> GetCommitsViaEventsAsync(string username, CancellationToken cancellationToken)
    {
        var total = 0;
        var cutoff = DateTime.UtcNow.AddYears(-1);

        for (var page = 1; page <= 3; page++)
        {
            try
            {
                var events = await _httpClient.GetFromJsonAsync<List<GitHubEventResponse>>(
                    $"users/{username}/events?per_page=100&page={page}", cancellationToken);

                if (events is null || events.Count == 0)
                    break;

                var pageCommits = events
                    .Where(e => e.Type == "PushEvent" && e.CreatedAt >= cutoff)
                    .Sum(e => e.Payload?.Size ?? 0);

                total += pageCommits;

                if (events.Last().CreatedAt < cutoff)
                    break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Events page {Page} failed for {Username}", page, username);
                break;
            }
        }

        return total;
    }
}
