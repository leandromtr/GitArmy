namespace GitArmy.Domain.ValueObjects;

public sealed class Score
{
    // Incrementar sempre que a fórmula ou as métricas de entrada mudarem: perfis gravados com outra versão
    // não têm as métricas novas e deixam de ser comparáveis, por isso são reanalisados.
    public const int CurrentVersion = 5;

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
        // Volume de commits (raiz quadrada: cada commit extra vale menos, sem teto precoce) + consistência
        // (semanas do último ano com pelo menos 1 contribuição).
        var commitScore      = p.CommitSaturate > 0
            ? p.CommitCap * Math.Sqrt(Math.Min(Math.Max(totalCommits, 0) / p.CommitSaturate, 1.0))
            : 0;
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

        // Liderança / Colaboração (max 25) — escala logarítmica: os primeiros PRs / seguidores valem mais
        // e o teto só se atinge com volumes realmente altos.
        var follScore  = LogScale(followers, p.FollowersSaturate) * p.FollowersCap;
        var colaborRaw = LogScale(
            totalPRsCreated * p.PrCreatedPts + totalPRsMerged * p.PrMergedPts + reviewsDone * p.ReviewPts,
            p.ColaborSaturate) * p.ColaborCap;
        var leaderRaw  = Math.Min(follScore + colaborRaw, 25.0);

        // Experiência (max 15)
        // activeYears = anos civis com atividade real (≥ ActiveYearMinContributions contribuições), não a idade da conta.
        var seniorityRaw = Math.Min(activeYears * p.ActiveYearPts, p.AntiguidadeCap);

        // Originalidade (max 15)
        // publicRepos = repositórios próprios, não-fork e com código real (ver RepoMinCodeBytes).
        var repoScore   = LogScale(publicRepos, p.RepoSaturate) * p.RepoCap;
        var starsScore  = LogScale(totalStars, p.StarsSaturate) * p.StarsCap;
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

    // Escala logarítmica normalizada: 0 → 0 e `saturate` → 1 (acima disso fica em 1).
    private static double LogScale(double value, double saturate) =>
        saturate <= 0 ? 0 : Math.Min(Math.Log10(Math.Max(value, 0) + 1) / Math.Log10(saturate + 1), 1.0);

    // Linha do tempo da substituição: a singularidade (AGI) chega em 2040 e o ano estimado de cada perfil
    // fica antes dela, entre 2027 (score 0) e 2038 (score 100). É a única fonte destes valores: a API
    // expõe-nos (ProfileDto e /api/scoring-rules) e o front não os repete.
    public const int SingularityYear        = 2040;
    public const int SubstitutionBaseYear   = 2027;
    public const int SubstitutionSpanYears  = 11;

    public static int SubstitutionYearFor(int score) =>
        SubstitutionBaseYear + (int)Math.Round(Math.Clamp(score, 0, 100) / 100.0 * SubstitutionSpanYears);

    public int ToSubstitutionYear() => SubstitutionYearFor(Value);
}
