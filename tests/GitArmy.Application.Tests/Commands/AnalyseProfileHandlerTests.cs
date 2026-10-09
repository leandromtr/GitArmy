using FluentAssertions;
using GitArmy.Application.Commands.AnalyseProfile;
using GitArmy.Application.Configuration;
using GitArmy.Application.Interfaces;
using GitArmy.Domain.Entities;
using GitArmy.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Moq;

namespace GitArmy.Application.Tests.Commands;

public class AnalyseProfileHandlerTests
{
    private readonly Mock<IGitHubClient> _gitHubClient = new();
    private readonly Mock<IProfileRepository> _repository = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly IOptions<ScoringSettings> _settings = Options.Create(new ScoringSettings());

    private AnalyseProfileHandler CreateHandler() =>
        new(_gitHubClient.Object, _repository.Object, _cache.Object, _settings);

    private static GitHubRepoData Repo(int stars = 0, bool empty = false, params (string Language, long Bytes)[] languages) =>
        new(stars, empty, languages.ToDictionary(l => l.Language, l => l.Bytes));

    private static GitHubProfileData Data(
        string username, IReadOnlyList<GitHubRepoData> repos, IReadOnlyList<int>? yearly = null,
        int commits = 0, int activeWeeks = 0, int followers = 0, int yearsOld = 3) =>
        new(Username: username, Followers: followers, CreatedAt: DateTime.UtcNow.AddYears(-yearsOld),
            Repos: repos, TotalCommitsLastYear: commits, ActiveWeeksLastYear: activeWeeks,
            YearlyContributions: yearly ?? [], TotalPRsCreated: 0, TotalPRsMerged: 0);

    private async Task<GitArmy.Application.DTOs.ProfileDto> Analyse(GitHubProfileData data)
    {
        _gitHubClient
            .Setup(c => c.GetProfileAsync(data.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);
        _repository
            .Setup(r => r.GetByUsernameAsync(data.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Profile?)null);

        return await CreateHandler().Handle(new AnalyseProfileCommand(data.Username), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_NewProfile_CreatesAndReturnsDto()
    {
        var githubData = new GitHubProfileData(
            Username: "testuser",
            Followers: 10,
            CreatedAt: DateTime.UtcNow.AddYears(-3),
            Repos: [Repo(15, false, ("C#", 40_000), ("Python", 25_000))],
            TotalCommitsLastYear: 50,
            ActiveWeeksLastYear: 20,
            YearlyContributions: [40, 25, 30],
            TotalPRsCreated: 2,
            TotalPRsMerged: 1);

        _gitHubClient
            .Setup(c => c.GetProfileAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(githubData);

        _repository
            .Setup(r => r.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Profile?)null);

        _cache.Setup(c => c.Get<object>(It.IsAny<string>())).Returns(null as object);

        var handler = CreateHandler();
        var result = await handler.Handle(new AnalyseProfileCommand("testuser"), CancellationToken.None);

        result.Should().NotBeNull();
        result.Username.Should().Be("testuser");
        result.UniqueLanguages.Should().Be(2);
        result.PublicRepos.Should().Be(1);
        result.DiversityScore.Should().BeGreaterThanOrEqualTo(0);
        _repository.Verify(r => r.UpsertAsync(It.IsAny<Profile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Experiência (anos ativos) ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_OldAccountWithoutActivity_GetsNoExperience()
    {
        var result = await Analyse(Data("dormant", [], yearly: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0], yearsOld: 9));

        result.AccountAgeYears.Should().Be(9);
        result.ActiveYears.Should().Be(0);
        result.SeniorityScore.Should().Be(0);
        result.Score.Should().Be(0);
    }

    [Fact]
    public async Task Handle_OnlyYearsAboveTheMinimumCountAsActive()
    {
        // Mínimo configurado: 12 contribuições/ano. Anos com 0, 1 e 11 não contam; 12 e 300 contam.
        var result = await Analyse(Data("sporadic", [], yearly: [0, 1, 11, 12, 300, 0], yearsOld: 5));

        result.ActiveYears.Should().Be(2);
        result.SeniorityScore.Should().Be(3); // 2 anos × 1,5 pts
    }

    // ── Substância: repositórios e linguagens ─────────────────────────────────

    [Fact]
    public async Task Handle_HelloWorldRepos_DoNotCountAsReposOrLanguages()
    {
        // 8 repositórios com poucas linhas em 7 linguagens: antes davam ~17/20 em Diversidade.
        var repos = new[]
        {
            Repo(0, false, ("JavaScript", 150)), Repo(0, false, ("Python", 90)), Repo(0, false, ("Go", 200)),
            Repo(0, false, ("Rust", 120)),       Repo(0, false, ("C#", 300)),    Repo(0, false, ("Java", 250)),
            Repo(0, false, ("Ruby", 80)),        Repo(0, false, ("PHP", 60)),
        };

        var result = await Analyse(Data("farmer", repos));

        result.PublicRepos.Should().Be(0);
        result.UniqueLanguages.Should().Be(0);
        result.DiversityScore.Should().Be(0);
        result.OriginalityScore.Should().Be(0);
    }

    [Fact]
    public async Task Handle_EmptyRepos_AreIgnored()
    {
        var result = await Analyse(Data("empties", [Repo(0, true), Repo(0, true, ("Python", 50_000))]));

        result.PublicRepos.Should().Be(0);
        result.UniqueLanguages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_LanguageNeedsTheMinimumVolumeOfCode_SummedAcrossRepos()
    {
        // Go: 6.000 + 6.000 bytes (≥ 10.000 no total) conta. Rust: só 3.000 bytes, não conta.
        var repos = new[]
        {
            Repo(0, false, ("Go", 6_000)),
            Repo(0, false, ("Go", 6_000)),
            Repo(0, false, ("Rust", 3_000)),
        };

        var result = await Analyse(Data("polyglot", repos));

        result.PublicRepos.Should().Be(3);
        result.UniqueLanguages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_HtmlAndStyleLanguages_CountAsASingleLanguage()
    {
        var repos = new[]
        {
            Repo(0, false, ("JavaScript", 30_000), ("TypeScript", 20_000), ("HTML", 20_000), ("CSS", 20_000), ("SCSS", 15_000)),
        };

        var result = await Analyse(Data("frontend", repos));

        // JavaScript+TypeScript = 1; HTML+CSS+SCSS = 1.
        result.UniqueLanguages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_StarsFromDocumentationOnlyRepos_StillCountForOriginality()
    {
        // Uma "awesome list" (sem código) não conta como repo, mas as estrelas contam.
        var result = await Analyse(Data("curator", [Repo(5_000, false)]));

        result.PublicRepos.Should().Be(0);
        result.OriginalityScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsInvalidOperationException()
    {
        _gitHubClient
            .Setup(c => c.GetProfileAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitHubProfileData?)null);

        _cache.Setup(c => c.Get<object>(It.IsAny<string>())).Returns(null as object);

        var handler = CreateHandler();
        var act = () => handler.Handle(new AnalyseProfileCommand("ghost"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_CachedProfile_DoesNotCallGitHub()
    {
        var cached = new GitArmy.Application.DTOs.ProfileDto(
            Username: "cached", Score: 70, AccountAgeYears: 3, UniqueLanguages: 2,
            Followers: 10, PublicRepos: 5, TotalCommitsLastYear: 50,
            ActiveWeeksLastYear: 20, ActiveYears: 3,
            SubstitutionYear: 2035, SingularityYear: 2040, ProfileName: "name", AgiThreatLevel: "ABORTED",
            FormationTier: "Calejado", FormationShape: "Cunha de Massa Crescente",
            TerrainTier: "Versátil", TerrainName: "Controlado",
            ActivityScore: 5, DiversityScore: 3, CollaborationScore: 8,
            SeniorityScore: 4, OriginalityScore: 10,
            ProcessedAt: DateTime.UtcNow,
            FormationDescription: "desc", TerrainDescription: "desc", AgiMessage: "msg");

        _cache.Setup(c => c.Get<GitArmy.Application.DTOs.ProfileDto>("profile:cached")).Returns(cached);

        var handler = CreateHandler();
        var result = await handler.Handle(new AnalyseProfileCommand("cached"), CancellationToken.None);

        result.Should().Be(cached);
        _gitHubClient.Verify(c => c.GetProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
