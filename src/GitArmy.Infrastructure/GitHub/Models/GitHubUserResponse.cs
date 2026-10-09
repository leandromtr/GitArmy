using System.Text.Json.Serialization;

namespace GitArmy.Infrastructure.GitHub.Models;

internal record GitHubUserResponse(
    [property: JsonPropertyName("login")]        string Login,
    [property: JsonPropertyName("public_repos")] int PublicRepos,
    [property: JsonPropertyName("followers")]    int Followers,
    [property: JsonPropertyName("created_at")]   DateTime CreatedAt);

internal record GitHubSearchResponse(
    [property: JsonPropertyName("total_count")] int TotalCount);
