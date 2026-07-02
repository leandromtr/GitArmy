namespace GitArmy.Domain.Entities;

public class Profile
{
    public Guid Id                  { get; private set; }
    public string Username          { get; private set; } = default!;
    public int Score                { get; private set; }
    public int AccountAgeYears      { get; private set; }
    public int UniqueLanguages      { get; private set; }
    public int Followers            { get; private set; }
    public int PublicRepos          { get; private set; }
    public int TotalCommitsLastYear { get; private set; }
    public int TotalPRsCreated      { get; private set; }
    public int TotalPRsMerged       { get; private set; }
    public int TotalStars           { get; private set; }
    public double DomainWeightSum   { get; private set; }
    public int SubstitutionYear     { get; private set; }
    public DateTime ProcessedAt     { get; private set; }

    private Profile() { }

    public static Profile Create(
        string username,
        int score,
        int accountAgeYears,
        int uniqueLanguages,
        int followers,
        int publicRepos,
        int totalCommitsLastYear,
        int totalPRsCreated,
        int totalPRsMerged,
        int totalStars,
        double domainWeightSum,
        int substitutionYear)
    {
        return new Profile
        {
            Id                  = Guid.NewGuid(),
            Username            = username.ToLowerInvariant(),
            Score               = score,
            AccountAgeYears     = accountAgeYears,
            UniqueLanguages     = uniqueLanguages,
            Followers           = followers,
            PublicRepos         = publicRepos,
            TotalCommitsLastYear = totalCommitsLastYear,
            TotalPRsCreated     = totalPRsCreated,
            TotalPRsMerged      = totalPRsMerged,
            TotalStars          = totalStars,
            DomainWeightSum     = domainWeightSum,
            SubstitutionYear    = substitutionYear,
            ProcessedAt         = DateTime.UtcNow,
        };
    }

    public void UpdateScores(
        int score,
        int accountAgeYears,
        int uniqueLanguages,
        int followers,
        int publicRepos,
        int totalCommitsLastYear,
        int totalPRsCreated,
        int totalPRsMerged,
        int totalStars,
        double domainWeightSum,
        int substitutionYear)
    {
        Score               = score;
        AccountAgeYears     = accountAgeYears;
        UniqueLanguages     = uniqueLanguages;
        Followers           = followers;
        PublicRepos         = publicRepos;
        TotalCommitsLastYear = totalCommitsLastYear;
        TotalPRsCreated     = totalPRsCreated;
        TotalPRsMerged      = totalPRsMerged;
        TotalStars          = totalStars;
        DomainWeightSum     = domainWeightSum;
        SubstitutionYear    = substitutionYear;
        ProcessedAt         = DateTime.UtcNow;
    }
}
