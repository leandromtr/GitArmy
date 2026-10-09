namespace GitArmy.Domain.ValueObjects;

public sealed record ScoreParameters(
    double CommitSaturate,   double CommitCap,
    double ConsistencyWeeks, double ConsistencyCap,
    double LangPointEach,    double LangCap,
    double DomainCap,        double DomainMaxWeight,
    double FollowersSaturate, double FollowersCap,
    double PrCreatedPts,     double PrMergedPts,
    double ReviewPts,        double ColaborSaturate, double ColaborCap,
    double ActiveYearPts,    double AntiguidadeCap,
    double RepoSaturate,     double RepoCap,
    double StarsSaturate,    double StarsCap);
