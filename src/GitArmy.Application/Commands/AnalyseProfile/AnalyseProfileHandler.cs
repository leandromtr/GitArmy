using GitArmy.Application.Common;
using GitArmy.Application.Configuration;
using GitArmy.Application.DTOs;
using GitArmy.Application.Interfaces;
using GitArmy.Domain.Entities;
using GitArmy.Domain.Interfaces;
using GitArmy.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

namespace GitArmy.Application.Commands.AnalyseProfile;

internal sealed class AnalyseProfileHandler : IRequestHandler<AnalyseProfileCommand, ProfileDto>
{
    private readonly IGitHubClient _gitHubClient;
    private readonly IProfileRepository _profileRepository;
    private readonly ICacheService _cacheService;
    private readonly IOptions<ScoringSettings> _settings;

    public AnalyseProfileHandler(
        IGitHubClient gitHubClient,
        IProfileRepository profileRepository,
        ICacheService cacheService,
        IOptions<ScoringSettings> settings)
    {
        _gitHubClient      = gitHubClient;
        _profileRepository = profileRepository;
        _cacheService      = cacheService;
        _settings          = settings;
    }

    public async Task<ProfileDto> Handle(AnalyseProfileCommand request, CancellationToken cancellationToken)
    {
        var username = GitHubUsername.Create(request.Username);
        var cacheKey = $"profile:{username.Value}";

        var cached = _cacheService.Get<ProfileDto>(cacheKey);
        if (cached is not null)
            return cached;

        var githubData = await _gitHubClient.GetProfileAsync(username.Value, cancellationToken)
            ?? throw new InvalidOperationException($"GitHub profile '{username.Value}' not found or is private.");

        var cfg                = _settings.Value;
        var scoreParams        = cfg.ToScoreParameters();
        var accountAgeYears    = (int)((DateTime.UtcNow - githubData.CreatedAt).TotalDays / 365);
        var activeYears        = githubData.YearlyContributions.Count(c => c >= cfg.ActiveYearMinContributions);
        var normalizedLangs    = ProfileClassifier.NormalizeLanguages(githubData.Languages);
        var domainWeightSum    = ProfileClassifier.GetDomainWeightSum(normalizedLangs, cfg.DomainWeights);

        var score = Score.Calculate(
            totalCommits:    githubData.TotalCommitsLastYear,
            activeWeeks:     githubData.ActiveWeeksLastYear,
            uniqueLanguages: normalizedLangs.Count,
            domainWeightSum: domainWeightSum,
            followers:       githubData.Followers,
            totalPRsCreated: githubData.TotalPRsCreated,
            totalPRsMerged:  githubData.TotalPRsMerged,
            reviewsDone:     0,                            // deferred — requires GraphQL contributions query
            activeYears:     activeYears,
            publicRepos:     githubData.PublicRepos,
            totalStars:      githubData.TotalStars,
            p:               scoreParams);

        var existing = await _profileRepository.GetByUsernameAsync(username.Value, cancellationToken);

        Profile profile;
        if (existing is null)
        {
            profile = Profile.Create(
                username:            username.Value,
                score:               score.Value,
                accountAgeYears:     accountAgeYears,
                uniqueLanguages:     normalizedLangs.Count,
                followers:           githubData.Followers,
                publicRepos:         githubData.PublicRepos,
                totalCommitsLastYear: githubData.TotalCommitsLastYear,
                activeWeeksLastYear: githubData.ActiveWeeksLastYear,
                activeYears:         activeYears,
                totalPRsCreated:     githubData.TotalPRsCreated,
                totalPRsMerged:      githubData.TotalPRsMerged,
                totalStars:          githubData.TotalStars,
                domainWeightSum:     domainWeightSum,
                substitutionYear:    score.ToSubstitutionYear(),
                scoreVersion:        Score.CurrentVersion);
        }
        else
        {
            existing.UpdateScores(
                score:               score.Value,
                accountAgeYears:     accountAgeYears,
                uniqueLanguages:     normalizedLangs.Count,
                followers:           githubData.Followers,
                publicRepos:         githubData.PublicRepos,
                totalCommitsLastYear: githubData.TotalCommitsLastYear,
                activeWeeksLastYear: githubData.ActiveWeeksLastYear,
                activeYears:         activeYears,
                totalPRsCreated:     githubData.TotalPRsCreated,
                totalPRsMerged:      githubData.TotalPRsMerged,
                totalStars:          githubData.TotalStars,
                domainWeightSum:     domainWeightSum,
                substitutionYear:    score.ToSubstitutionYear(),
                scoreVersion:        Score.CurrentVersion);
            profile = existing;
        }

        await _profileRepository.UpsertAsync(profile, cancellationToken);

        var dto = ToDto(profile, score);
        _cacheService.Set(cacheKey, dto, TimeSpan.FromHours(1));
        return dto;
    }

    private static ProfileDto ToDto(Profile p, Score score) => new(
        Username:            p.Username,
        Score:               p.Score,
        AccountAgeYears:     p.AccountAgeYears,
        UniqueLanguages:     p.UniqueLanguages,
        Followers:           p.Followers,
        PublicRepos:         p.PublicRepos,
        TotalCommitsLastYear: p.TotalCommitsLastYear,
        ActiveWeeksLastYear: p.ActiveWeeksLastYear,
        ActiveYears:         p.ActiveYears,
        SubstitutionYear:    p.SubstitutionYear,
        SingularityYear:     Score.SingularityYear,
        ProfileName:         ProfileClassifier.GetProfileName(p.Score),
        AgiThreatLevel:      ProfileClassifier.GetAgiThreatLevel(p.Score),
        FormationTier:       ProfileClassifier.GetFormationTier(p.AccountAgeYears),
        FormationShape:      ProfileClassifier.GetFormationShape(p.AccountAgeYears),
        TerrainTier:         ProfileClassifier.GetTerrainTier(p.UniqueLanguages),
        TerrainName:         ProfileClassifier.GetTerrainName(p.UniqueLanguages),
        ActivityScore:       score.ActivityScore,
        DiversityScore:      score.DiversityScore,
        CollaborationScore:  score.CollaborationScore,
        SeniorityScore:      score.SeniorityScore,
        OriginalityScore:    score.OriginalityScore,
        ProcessedAt:         p.ProcessedAt,
        FormationDescription: ProfileClassifier.GetFormationDescription(p.AccountAgeYears),
        TerrainDescription:  ProfileClassifier.GetTerrainDescription(p.UniqueLanguages),
        AgiMessage:          ProfileClassifier.GetAgiMessage(p.Score));
}
