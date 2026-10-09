using GitArmy.Domain.ValueObjects;

namespace GitArmy.Application.Configuration;

public sealed class ScoringSettings
{
    public const string Section = "Scoring";

    // Atividade (max 25)
    public double CommitSaturate   { get; init; } = 2000; // commits/ano para o teto de volume (escala √)
    public double CommitCap        { get; init; } = 15;
    public double ConsistencyWeeks { get; init; } = 52;
    public double ConsistencyCap   { get; init; } = 10;

    // Diversidade Técnica (max 20)
    public double LangPointEach { get; init; } = 1.0;
    public double LangCap       { get; init; } = 7;
    public double DomainCap     { get; init; } = 13;
    // Uma linguagem só conta com código real: bytes mínimos somados nos repositórios próprios com substância.
    // Evita somar "linguagens" com um hello-world de cada.
    public long LangMinBytes { get; init; } = 10_000;
    // O bónus de domínios satura ao cobrir os N domínios de maior peso (e não todos os 6), para que um perfil
    // com stack típica (ex.: front + back + devops) possa chegar perto do teto.
    public int DomainSaturateCount { get; init; } = 4;
    public Dictionary<string, double> DomainWeights { get; init; } = new()
    {
        ["frontend"]        = 1.00,
        ["backend"]         = 1.25,
        ["devops"]          = 1.25,
        ["ml_ai"]           = 1.50,
        ["systems"]         = 1.75,
        ["domain_specific"] = 2.00,
    };

    // Liderança / Colaboração (max 25)
    public double FollowersSaturate { get; init; } = 10_000;
    public double FollowersCap      { get; init; } = 10;
    public double PrCreatedPts      { get; init; } = 0.25;
    public double PrMergedPts       { get; init; } = 0.75;
    public double ReviewPts         { get; init; } = 0.80;
    public double ColaborSaturate   { get; init; } = 200; // "pontos de PR" para o teto (escala log)
    public double ColaborCap        { get; init; } = 15;

    // Experiência (max 15) — anos civis com atividade real, não a idade da conta
    public double ActiveYearPts  { get; init; } = 1.5;
    public double AntiguidadeCap { get; init; } = 15;
    // Um ano só conta como "activo" a partir deste nº de contribuições (≈ 1 por mês); com 1 só, um commit
    // por ano chegava para somar anos.
    public int ActiveYearMinContributions { get; init; } = 12;

    // Originalidade (max 15)
    public double RepoSaturate { get; init; } = 30;     // repos com substância para o teto (escala log)
    public double RepoCap      { get; init; } = 8;
    // Um repositório só conta com código real: bytes mínimos (≈ 50–100 linhas). Exclui vazios e hello-worlds.
    public long RepoMinCodeBytes { get; init; } = 2_000;
    public double StarsSaturate { get; init; } = 10_000;
    public double StarsCap      { get; init; } = 7;

    // Soma dos pesos dos N domínios mais pesados: é o "100 %" do bónus de domínios.
    public double ComputedDomainMaxWeight =>
        DomainWeights.Values.OrderByDescending(w => w).Take(Math.Max(DomainSaturateCount, 1)).Sum();

    public ScoreParameters ToScoreParameters() => new(
        CommitSaturate,   CommitCap,
        ConsistencyWeeks, ConsistencyCap,
        LangPointEach,    LangCap,
        DomainCap,        ComputedDomainMaxWeight,
        FollowersSaturate, FollowersCap,
        PrCreatedPts,     PrMergedPts,
        ReviewPts,        ColaborSaturate, ColaborCap,
        ActiveYearPts,    AntiguidadeCap,
        RepoSaturate,     RepoCap,
        StarsSaturate,    StarsCap);
}
