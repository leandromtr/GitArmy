using System.Text.RegularExpressions;

namespace GitArmy.Domain.ValueObjects;

public sealed class GitHubUsername
{
    private static readonly Regex ValidPattern =
        new(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,37}[a-zA-Z0-9])?$", RegexOptions.Compiled);

    public string Value { get; }

    private GitHubUsername(string value) => Value = value;

    public static GitHubUsername Create(string input)
    {
        var normalized = input?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrEmpty(normalized))
            throw new ArgumentException("Username cannot be empty.");

        if (normalized.Length > 39)
            throw new ArgumentException("Username cannot exceed 39 characters.");

        if (normalized.Contains("--"))
            throw new ArgumentException("Username cannot contain consecutive hyphens.");

        if (!ValidPattern.IsMatch(normalized))
            throw new ArgumentException($"'{normalized}' is not a valid GitHub username.");

        return new GitHubUsername(normalized);
    }

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is GitHubUsername other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
}
