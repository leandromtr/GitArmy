namespace GitArmy.Domain.ValueObjects;

public sealed record ScoreParameters(
    double CommitMultiplier, double CommitCap,
    double QualitySaturate,  double QualityCap,
    double LangPointEach,    double LangCap,
    double DomainCap,        double DomainMaxWeight,
    double FollowersSaturate, double FollowersCap,
    double PrCreatedPts,     double PrMergedPts,
    double ReviewPts,        double ColaborCap,
    double ActiveYearPts,    double AntiguidadeCap,
    double RepoPts,          double RepoCap,
    double StarsSaturate,    double StarsCap);
