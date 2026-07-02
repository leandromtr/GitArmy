using System.Text.Json.Serialization;

namespace GitArmy.Infrastructure.GitHub.Models;

internal record GitHubUserResponse(
    [property: JsonPropertyName("login")]        string Login,
    [property: JsonPropertyName("public_repos")] int PublicRepos,
    [property: JsonPropertyName("followers")]    int Followers,
    [property: JsonPropertyName("created_at")]   DateTime CreatedAt);

internal record GitHubRepoResponse(
    [property: JsonPropertyName("language")]         string? Language,
    [property: JsonPropertyName("fork")]             bool Fork,
    [property: JsonPropertyName("stargazers_count")] int StargazersCount);

internal record GitHubEventResponse(
    [property: JsonPropertyName("type")]       string Type,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("payload")]    GitHubPushPayload? Payload);

internal record GitHubPushPayload(
    [property: JsonPropertyName("size")] int Size);

internal record GitHubSearchResponse(
    [property: JsonPropertyName("total_count")] int TotalCount);
