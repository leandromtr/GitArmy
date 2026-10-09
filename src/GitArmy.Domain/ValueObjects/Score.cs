namespace GitArmy.Domain.ValueObjects;

public sealed class Score
{
    // Incrementar sempre que a fórmula ou as métricas de entrada mudarem: perfis gravados com outra versão
    // não têm as métricas novas e deixam de ser comparáveis, por isso são reanalisados.
    public const int CurrentVersion = 4;

    public int Value              { get; }
    public int ActivityScore      { get; } // max 25
    public int DiversityScore     { get; } // max 20
    public int CollaborationScore { get; } // max 25
    public int SeniorityScore     { get; } // max 15
    public int OriginalityScore   { get; } // max 15

    private Score(int value, int activity, int diversity, int collab, int seniority, int originality)
    {
        Value             = value;
        ActivityScore     = activity;
        DiversityScore    = diversity;
        CollaborationScore = collab;
        SeniorityScore    = seniority;
        OriginalityScore  = originality;
    }

    public static Score Calculate(
        int totalCommits,    int activeWeeks,
        int uniqueLanguages, double domainWeightSum,
        int followers,       int totalPRsCreated,
        int totalPRsMerged,  int reviewsDone,
        int activeYears,     int publicRepos,
        int totalStars,      ScoreParameters p)
    {
        // Atividade (max 25)
        // Volume de commits + consistência (semanas do último ano com pelo menos 1 contribuição).
        var commitScore      = Math.Min(totalCommits * p.CommitMultiplier, p.CommitCap);
        var consistencyScore = p.ConsistencyWeeks > 0
            ? Math.Min(activeWeeks / p.ConsistencyWeeks * p.ConsistencyCap, p.ConsistencyCap)
            : 0;
        var activityRaw = Math.Min(commitScore + consistencyScore, 25.0);

        // Diversidade Técnica (max 20)
        var langScore    = Math.Min(uniqueLanguages * p.LangPointEach, p.LangCap);
        var domainScore  = p.DomainMaxWeight > 0
            ? Math.Min(domainWeightSum / p.DomainMaxWeight * p.DomainCap, p.DomainCap)
            : 0;
        var diversityRaw = Math.Min(langScore + domainScore, 20.0);

        // Liderança / Colaboração (max 25)
        var follScore  = Math.Min(
            Math.Log10(followers + 1) / Math.Log10(p.FollowersSaturate + 1) * p.FollowersCap,
            p.FollowersCap);
        var colaborRaw = Math.Min(
            totalPRsCreated * p.PrCreatedPts + totalPRsMerged * p.PrMergedPts + reviewsDone * p.ReviewPts,
            p.ColaborCap);
        var leaderRaw  = Math.Min(follScore + colaborRaw, 25.0);

        // Experiência (max 15)
        // activeYears = anos civis com atividade real (≥ ActiveYearMinContributions contribuições), não a idade da conta.
        var seniorityRaw = Math.Min(activeYears * p.ActiveYearPts, p.AntiguidadeCap);

        // Originalidade (max 15)
        var repoScore   = Math.Min(publicRepos * p.RepoPts, p.RepoCap);
        var starsScore  = Math.Min(
            Math.Log10(totalStars + 1) / Math.Log10(p.StarsSaturate + 1) * p.StarsCap,
            p.StarsCap);
        var originalRaw = Math.Min(repoScore + starsScore, 15.0);

        var total = Math.Clamp(
            (int)Math.Round(activityRaw + diversityRaw + leaderRaw + seniorityRaw + originalRaw),
            0, 100);

        return new Score(
            value:       total,
            activity:    (int)Math.Round(activityRaw),
            diversity:   (int)Math.Round(diversityRaw),
            collab:      (int)Math.Round(leaderRaw),
            seniority:   (int)Math.Round(seniorityRaw),
            originality: (int)Math.Round(originalRaw));
    }

    public int ToSubstitutionYear() => 2028 + (int)Math.Round(Value / 100.0 * 19.0);
}
