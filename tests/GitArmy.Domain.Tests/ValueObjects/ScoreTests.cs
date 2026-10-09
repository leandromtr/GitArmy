using FluentAssertions;
using GitArmy.Domain.ValueObjects;

namespace GitArmy.Domain.Tests.ValueObjects;

public class ScoreTests
{
    // Mesmos valores de appsettings.json / ScoringSettings. DomainMaxWeight = soma dos 4 domínios mais pesados.
    private static readonly ScoreParameters Params = new(
        CommitSaturate: 2000,    CommitCap: 15,
        ConsistencyWeeks: 52,    ConsistencyCap: 10,
        LangPointEach: 1.0,      LangCap: 7,
        DomainCap: 13,           DomainMaxWeight: 6.5,
        FollowersSaturate: 10000, FollowersCap: 10,
        PrCreatedPts: 0.25,      PrMergedPts: 0.75,
        ReviewPts: 0.8,          ColaborSaturate: 200, ColaborCap: 15,
        ActiveYearPts: 1.5,      AntiguidadeCap: 15,
        RepoSaturate: 30,        RepoCap: 8,
        StarsSaturate: 10000,    StarsCap: 7);

    private static Score Calc(
        int commits = 0, int activeWeeks = 0, int languages = 0, double domainWeightSum = 0,
        int followers = 0, int prs = 0, int merged = 0, int reviews = 0,
        int activeYears = 0, int repos = 0, int stars = 0) =>
        Score.Calculate(commits, activeWeeks, languages, domainWeightSum, followers, prs, merged,
            reviews, activeYears, repos, stars, Params);

    [Fact]
    public void Calculate_EmptyProfile_ScoresZero()
    {
        Calc().Value.Should().Be(0);
    }

    // ── Atividade ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(20, 2)]     // 15 × √(20/2000)  = 1,5
    [InlineData(80, 3)]     // 15 × √(80/2000)  = 3
    [InlineData(720, 9)]    // 15 × √(720/2000) = 9
    [InlineData(2000, 15)]
    [InlineData(3787, 15)]
    public void Calculate_Activity_CommitVolumeUsesSquareRootScale(int commits, int expected)
    {
        Calc(commits: commits).ActivityScore.Should().Be(expected);
    }

    [Fact]
    public void Calculate_Activity_300CommitsNoLongerSaturate()
    {
        // Antes 300 commits/ano já davam o máximo do volume (15 pts) e não se distinguiam de 3.787.
        Calc(commits: 300).ActivityScore.Should().BeLessThan(15);
        Calc(commits: 3787).ActivityScore.Should().Be(15);
    }

    [Fact]
    public void Calculate_Activity_ConsistencyUsesActiveWeeks()
    {
        Calc(activeWeeks: 26).ActivityScore.Should().Be(5);
        Calc(activeWeeks: 52).ActivityScore.Should().Be(10);
        Calc(activeWeeks: 53).ActivityScore.Should().Be(10, "o teto de consistência é 10 pts");
    }

    [Fact]
    public void Calculate_Activity_CanReachItsFull25Points()
    {
        Calc(commits: 2000, activeWeeks: 52).ActivityScore.Should().Be(25);
    }

    // ── Diversidade ───────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_Diversity_DomainBonusSaturatesWithTheFourHeaviestDomains()
    {
        Calc(domainWeightSum: 6.5).DiversityScore.Should().Be(13);
        Calc(domainWeightSum: 8.75, languages: 0).DiversityScore.Should().Be(13, "acima dos 4 domínios o bónus não cresce");
    }

    // ── Liderança ─────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_Leadership_FollowersUseLogScale()
    {
        Calc(followers: 99).CollaborationScore.Should().Be(5);      // log10(100) / log10(10001) × 10
        Calc(followers: 10_000).CollaborationScore.Should().Be(10);
        Calc(followers: 1_000_000).CollaborationScore.Should().Be(10);
    }

    [Fact]
    public void Calculate_Leadership_15MergedPrsNoLongerSaturate()
    {
        // Antes 15 PRs merged davam o teto de contribuições; agora é preciso um volume ~13× maior.
        Calc(prs: 15, merged: 15).CollaborationScore.Should().BeLessThan(15);
        Calc(prs: 800).CollaborationScore.Should().Be(15); // 800 × 0,25 = 200 "pontos de PR"
    }

    // ── Experiência ───────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_Seniority_UsesActiveYears()
    {
        Calc(activeYears: 4).SeniorityScore.Should().Be(6);
        Calc(activeYears: 10).SeniorityScore.Should().Be(15);
        Calc(activeYears: 25).SeniorityScore.Should().Be(15);
    }

    [Fact]
    public void Calculate_DormantAccount_GetsNoSeniority()
    {
        // Conta antiga sem atividade: 0 anos ativos → 0 pts de experiência.
        Calc(activeYears: 0).SeniorityScore.Should().Be(0);
    }

    // ── Originalidade ─────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_Originality_8ReposNoLongerSaturate()
    {
        Calc(repos: 8).OriginalityScore.Should().Be(5);   // log10(9) / log10(31) × 8
        Calc(repos: 30).OriginalityScore.Should().Be(8);
    }

    [Fact]
    public void Calculate_Originality_StarsUseLogScaleUpTo10000()
    {
        Calc(stars: 1000).OriginalityScore.Should().Be(5);   // 3/4 × 7 = 5,25
        Calc(stars: 10_000).OriginalityScore.Should().Be(7);
    }

    // ── Totais ────────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_MaximumProfile_ReachesExactly100()
    {
        var score = Calc(
            commits: 5000, activeWeeks: 52, languages: 20, domainWeightSum: 8.75,
            followers: 1_000_000, prs: 500, merged: 500, reviews: 0,
            activeYears: 15, repos: 100, stars: 1_000_000);

        score.ActivityScore.Should().Be(25);
        score.DiversityScore.Should().Be(20);
        score.CollaborationScore.Should().Be(25);
        score.SeniorityScore.Should().Be(15);
        score.OriginalityScore.Should().Be(15);
        score.Value.Should().Be(100);
    }

    [Fact]
    public void Calculate_ComponentsAddUpToTotal_ForRoundNumbers()
    {
        var score = Calc(commits: 80, activeWeeks: 26, activeYears: 6, repos: 30);

        (score.ActivityScore + score.DiversityScore + score.CollaborationScore +
         score.SeniorityScore + score.OriginalityScore).Should().Be(score.Value);
    }

    [Fact]
    public void Calculate_IsMonotonic_MoreOfAnythingNeverLowersTheScore()
    {
        var baseline = Calc(commits: 100, activeWeeks: 20, languages: 2, domainWeightSum: 2, followers: 10,
            prs: 5, merged: 3, activeYears: 3, repos: 4, stars: 20);

        Calc(commits: 400, activeWeeks: 20, languages: 2, domainWeightSum: 2, followers: 10,
            prs: 5, merged: 3, activeYears: 3, repos: 4, stars: 20).Value.Should().BeGreaterThanOrEqualTo(baseline.Value);
        Calc(commits: 100, activeWeeks: 40, languages: 2, domainWeightSum: 2, followers: 10,
            prs: 5, merged: 3, activeYears: 3, repos: 4, stars: 20).Value.Should().BeGreaterThanOrEqualTo(baseline.Value);
        Calc(commits: 100, activeWeeks: 20, languages: 4, domainWeightSum: 4, followers: 10,
            prs: 5, merged: 3, activeYears: 3, repos: 4, stars: 20).Value.Should().BeGreaterThanOrEqualTo(baseline.Value);
        Calc(commits: 100, activeWeeks: 20, languages: 2, domainWeightSum: 2, followers: 500,
            prs: 50, merged: 30, activeYears: 3, repos: 4, stars: 20).Value.Should().BeGreaterThanOrEqualTo(baseline.Value);
        Calc(commits: 100, activeWeeks: 20, languages: 2, domainWeightSum: 2, followers: 10,
            prs: 5, merged: 3, activeYears: 3, repos: 20, stars: 900).Value.Should().BeGreaterThanOrEqualTo(baseline.Value);
    }

    // ── Linha do tempo da substituição ────────────────────────────────────────

    [Fact]
    public void ToSubstitutionYear_IsMonotonicInScore()
    {
        var low  = Calc(commits: 50).ToSubstitutionYear();
        var high = Calc(commits: 5000, activeWeeks: 52, activeYears: 15, repos: 30).ToSubstitutionYear();

        high.Should().BeGreaterThan(low);
    }

    [Fact]
    public void SingularityYear_Is2040()
    {
        Score.SingularityYear.Should().Be(2040);
    }

    [Fact]
    public void SubstitutionYearFor_SpansFrom2027To2038()
    {
        Score.SubstitutionYearFor(0).Should().Be(2027);
        Score.SubstitutionYearFor(100).Should().Be(2038);
    }

    [Fact]
    public void SubstitutionYearFor_NeverReachesTheSingularity()
    {
        for (var score = 0; score <= 100; score++)
            Score.SubstitutionYearFor(score).Should().BeLessThan(Score.SingularityYear);
    }

    [Fact]
    public void SubstitutionYearFor_NeverDecreasesAsScoreGrows()
    {
        var previous = Score.SubstitutionYearFor(0);
        for (var score = 1; score <= 100; score++)
        {
            var year = Score.SubstitutionYearFor(score);
            year.Should().BeGreaterThanOrEqualTo(previous);
            previous = year;
        }
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(250)]
    public void SubstitutionYearFor_ClampsOutOfRangeScores(int score)
    {
        Score.SubstitutionYearFor(score).Should().BeInRange(2027, 2038);
    }
}
