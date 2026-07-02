using GitArmy.Domain.ValueObjects;

namespace GitArmy.Application.Configuration;

public sealed class ScoringSettings
{
    public const string Section = "Scoring";

    // Atividade (max 25)
    public double CommitMultiplier { get; init; } = 0.05;
    public double CommitCap        { get; init; } = 15;
    public double QualitySaturate  { get; init; } = 200;
    public double QualityCap       { get; init; } = 10;

    // Diversidade Técnica (max 20)
    public double LangPointEach { get; init; } = 1.0;
    public double LangCap       { get; init; } = 7;
    public double DomainCap     { get; init; } = 13;
    public Dictionary<string, double> DomainWeights { get; init; } = new()
    {
        ["ml_ai"]           = 1.00,
        ["frontend"]        = 1.00,
        ["backend"]         = 1.25,
        ["devops"]          = 1.50,
        ["systems"]         = 1.75,
        ["domain_specific"] = 2.00,
    };

    // Liderança / Colaboração (max 25)
    public double FollowersSaturate { get; init; } = 1000;
    public double FollowersCap      { get; init; } = 10;
    public double PrCreatedPts      { get; init; } = 0.25;
    public double PrMergedPts       { get; init; } = 0.75;
    public double ReviewPts         { get; init; } = 0.80;
    public double ColaborCap        { get; init; } = 15;

    // Antiguidade (max 15)
    public double ActiveYearPts  { get; init; } = 1.5;
    public double AntiguidadeCap { get; init; } = 15;

    // Originalidade (max 15)
    public double RepoPts       { get; init; } = 1.0;
    public double RepoCap       { get; init; } = 8;
    public double StarsSaturate { get; init; } = 1000;
    public double StarsCap      { get; init; } = 7;

    public double ComputedDomainMaxWeight => DomainWeights.Values.Sum();

    public ScoreParameters ToScoreParameters() => new(
        CommitMultiplier, CommitCap,
        QualitySaturate,  QualityCap,
        LangPointEach,    LangCap,
        DomainCap,        ComputedDomainMaxWeight,
        FollowersSaturate, FollowersCap,
        PrCreatedPts,     PrMergedPts,
        ReviewPts,        ColaborCap,
        ActiveYearPts,    AntiguidadeCap,
        RepoPts,          RepoCap,
        StarsSaturate,    StarsCap);
}
