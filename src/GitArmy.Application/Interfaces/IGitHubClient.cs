namespace GitArmy.Application.Interfaces;

public interface IGitHubClient
{
    Task<GitHubProfileData?> GetProfileAsync(string username, CancellationToken cancellationToken = default);
}

// Repositório próprio e não-fork. Os limiares de "substância" são regras de pontuação e ficam na Application.
public record GitHubRepoData(
    int Stars,
    bool IsEmpty,
    IReadOnlyDictionary<string, long> LanguageBytes); // bytes de código por linguagem

public record GitHubProfileData(
    string Username,
    int Followers,
    DateTime CreatedAt,
    IReadOnlyList<GitHubRepoData> Repos, // até 100, dos mais para os menos estrelados
    int TotalCommitsLastYear,
    int ActiveWeeksLastYear,
    IReadOnlyList<int> YearlyContributions, // total de contribuições por ano civil, do ano de criação da conta ao atual
    int TotalPRsCreated,
    int TotalPRsMerged);

// O GitHub não pôde ser consultado (token recusado, limite de pedidos, API em baixo).
// Falha de forma explícita em vez de pontuar com dados incompletos.
public sealed class GitHubUnavailableException : Exception
{
    public GitHubUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
