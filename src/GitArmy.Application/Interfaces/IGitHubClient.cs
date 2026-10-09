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
    int ActiveWeeksLastYear,
    IReadOnlyList<int> YearlyContributions, // total de contribuições por ano civil, do ano de criação da conta ao atual
    int TotalPRsCreated,
    int TotalPRsMerged,
    int TotalStars);

// O GitHub não pôde ser consultado (token recusado, limite de pedidos, API em baixo).
// Falha de forma explícita em vez de pontuar com dados incompletos.
public sealed class GitHubUnavailableException : Exception
{
    public GitHubUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
