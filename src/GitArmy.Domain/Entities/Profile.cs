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
    public int ActiveWeeksLastYear  { get; private set; }
    public int ActiveYears          { get; private set; }
    public int TotalPRsCreated      { get; private set; }
    public int TotalPRsMerged       { get; private set; }
    public int TotalStars           { get; private set; }
    public double DomainWeightSum   { get; private set; }
    public int SubstitutionYear     { get; private set; }
    public int ScoreVersion         { get; private set; }
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
        int activeWeeksLastYear,
        int activeYears,
        int totalPRsCreated,
        int totalPRsMerged,
        int totalStars,
        double domainWeightSum,
        int substitutionYear,
        int scoreVersion)
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
            ActiveWeeksLastYear = activeWeeksLastYear,
            ActiveYears         = activeYears,
            TotalPRsCreated     = totalPRsCreated,
            TotalPRsMerged      = totalPRsMerged,
            TotalStars          = totalStars,
            DomainWeightSum     = domainWeightSum,
            SubstitutionYear    = substitutionYear,
            ScoreVersion        = scoreVersion,
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
        int activeWeeksLastYear,
        int activeYears,
        int totalPRsCreated,
        int totalPRsMerged,
        int totalStars,
        double domainWeightSum,
        int substitutionYear,
        int scoreVersion)
    {
        Score               = score;
        AccountAgeYears     = accountAgeYears;
        UniqueLanguages     = uniqueLanguages;
        Followers           = followers;
        PublicRepos         = publicRepos;
        TotalCommitsLastYear = totalCommitsLastYear;
        ActiveWeeksLastYear = activeWeeksLastYear;
        ActiveYears         = activeYears;
        TotalPRsCreated     = totalPRsCreated;
        TotalPRsMerged      = totalPRsMerged;
        TotalStars          = totalStars;
        DomainWeightSum     = domainWeightSum;
        SubstitutionYear    = substitutionYear;
        ScoreVersion        = scoreVersion;
        ProcessedAt         = DateTime.UtcNow;
    }

    // Recalcula só o resultado a partir das métricas já guardadas (ex.: regras de pontuação alteradas),
    // sem tocar nos dados do GitHub nem na data da análise.
    public void Rescore(int score, int substitutionYear)
    {
        Score            = score;
        SubstitutionYear = substitutionYear;
    }
}
