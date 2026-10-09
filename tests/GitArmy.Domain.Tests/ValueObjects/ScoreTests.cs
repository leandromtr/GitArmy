using FluentAssertions;
using GitArmy.Domain.ValueObjects;

namespace GitArmy.Domain.Tests.ValueObjects;

public class ScoreTests
{
    // Mesmos valores de appsettings.json / ScoringSettings.
    private static readonly ScoreParameters Params = new(
        CommitMultiplier: 0.05, CommitCap: 15,
        ConsistencyWeeks: 52,   ConsistencyCap: 10,
        LangPointEach: 1.0,     LangCap: 7,
        DomainCap: 13,          DomainMaxWeight: 8.5,
        FollowersSaturate: 1000, FollowersCap: 10,
        PrCreatedPts: 0.25,     PrMergedPts: 0.75,
        ReviewPts: 0.8,         ColaborCap: 15,
        ActiveYearPts: 1.5,     AntiguidadeCap: 15,
        RepoPts: 1.0,           RepoCap: 8,
        StarsSaturate: 1000,    StarsCap: 7);

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

    [Fact]
    public void Calculate_Activity_CommitVolumeSaturatesAt300()
    {
        Calc(commits: 300).ActivityScore.Should().Be(15);
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
        Calc(commits: 300, activeWeeks: 52).ActivityScore.Should().Be(25);
    }

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

    [Fact]
    public void Calculate_MaximumProfile_ReachesExactly100()
    {
        var score = Calc(
            commits: 5000, activeWeeks: 52, languages: 20, domainWeightSum: 8.5,
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
        var score = Calc(commits: 100, activeWeeks: 26, activeYears: 6, repos: 4);

        (score.ActivityScore + score.DiversityScore + score.CollaborationScore +
         score.SeniorityScore + score.OriginalityScore).Should().Be(score.Value);
    }

    [Fact]
    public void ToSubstitutionYear_IsMonotonicInScore()
    {
        var low  = Calc(commits: 50).ToSubstitutionYear();
        var high = Calc(commits: 5000, activeWeeks: 52, activeYears: 15, repos: 8).ToSubstitutionYear();

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
