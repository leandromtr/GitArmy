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

    [Fact]
    public async Task Handle_NewProfile_CreatesAndReturnsDto()
    {
        var githubData = new GitHubProfileData(
            Username: "testuser",
            PublicRepos: 5,
            Followers: 10,
            CreatedAt: DateTime.UtcNow.AddYears(-3),
            Languages: ["C#", "Python"],
            TotalCommitsLastYear: 50,
            ActiveWeeksLastYear: 20,
            YearlyContributions: [40, 25, 30],
            TotalPRsCreated: 2,
            TotalPRsMerged: 1,
            TotalStars: 15);

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
        result.DiversityScore.Should().BeGreaterThanOrEqualTo(0);
        _repository.Verify(r => r.UpsertAsync(It.IsAny<Profile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OldAccountWithoutActivity_GetsNoExperience()
    {
        var githubData = new GitHubProfileData(
            Username: "dormant", PublicRepos: 0, Followers: 0,
            CreatedAt: DateTime.UtcNow.AddYears(-9), Languages: [],
            TotalCommitsLastYear: 0, ActiveWeeksLastYear: 0,
            YearlyContributions: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
            TotalPRsCreated: 0, TotalPRsMerged: 0, TotalStars: 0);

        _gitHubClient
            .Setup(c => c.GetProfileAsync("dormant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(githubData);
        _repository
            .Setup(r => r.GetByUsernameAsync("dormant", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Profile?)null);

        var result = await CreateHandler().Handle(new AnalyseProfileCommand("dormant"), CancellationToken.None);

        result.AccountAgeYears.Should().Be(9);
        result.ActiveYears.Should().Be(0);
        result.SeniorityScore.Should().Be(0);
        result.Score.Should().Be(0);
    }

    [Fact]
    public async Task Handle_OnlyYearsAboveTheMinimumCountAsActive()
    {
        // Mínimo configurado: 12 contribuições/ano. Anos com 0, 1 e 11 não contam; 12 e 300 contam.
        var githubData = new GitHubProfileData(
            Username: "sporadic", PublicRepos: 0, Followers: 0,
            CreatedAt: DateTime.UtcNow.AddYears(-5), Languages: [],
            TotalCommitsLastYear: 0, ActiveWeeksLastYear: 0,
            YearlyContributions: [0, 1, 11, 12, 300, 0],
            TotalPRsCreated: 0, TotalPRsMerged: 0, TotalStars: 0);

        _gitHubClient
            .Setup(c => c.GetProfileAsync("sporadic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(githubData);
        _repository
            .Setup(r => r.GetByUsernameAsync("sporadic", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Profile?)null);

        var result = await CreateHandler().Handle(new AnalyseProfileCommand("sporadic"), CancellationToken.None);

        result.ActiveYears.Should().Be(2);
        result.SeniorityScore.Should().Be(3); // 2 anos × 1,5 pts
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
            SubstitutionYear: 2043, ProfileName: "name", AgiThreatLevel: "ABORTED",
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
