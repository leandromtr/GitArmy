namespace GitArmy.Application.Interfaces;

public interface IGitHubClient
{
    Task<GitHubProfileData?> GetProfileAsync(string username, CancellationToken cancellationToken = default);
}

public record GitHubProfileData(
    string Username,
    int PublicRepos,
    int Followers,
    DateTime CreatedAt,
    IReadOnlyList<string> Languages,
    int TotalCommitsLastYear,
    int TotalPRsCreated,
    int TotalPRsMerged,
    int TotalStars);
