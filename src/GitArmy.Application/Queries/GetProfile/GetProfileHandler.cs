using GitArmy.Application.Common;
using GitArmy.Application.Configuration;
using GitArmy.Application.DTOs;
using GitArmy.Domain.Interfaces;
using GitArmy.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

namespace GitArmy.Application.Queries.GetProfile;

internal sealed class GetProfileHandler : IRequestHandler<GetProfileQuery, ProfileDto?>
{
    private readonly IProfileRepository _profileRepository;
    private readonly ICacheService _cacheService;
    private readonly IOptions<ScoringSettings> _settings;

    public GetProfileHandler(
        IProfileRepository profileRepository,
        ICacheService cacheService,
        IOptions<ScoringSettings> settings)
    {
        _profileRepository = profileRepository;
        _cacheService      = cacheService;
        _settings          = settings;
    }

    public async Task<ProfileDto?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var username = GitHubUsername.Create(request.Username);
        var cacheKey = $"profile:{username.Value}";

        var cached = _cacheService.Get<ProfileDto>(cacheKey);
        if (cached is not null)
            return cached;

        var profile = await _profileRepository.GetByUsernameAsync(username.Value, cancellationToken);
        if (profile is null)
            return null;

        var scoreParams = _settings.Value.ToScoreParameters();

        var score = Score.Calculate(
            totalCommits:    profile.TotalCommitsLastYear,
            totalAdditions:  0,
            uniqueLanguages: profile.UniqueLanguages,
            domainWeightSum: profile.DomainWeightSum,
            followers:       profile.Followers,
            totalPRsCreated: profile.TotalPRsCreated,
            totalPRsMerged:  profile.TotalPRsMerged,
            reviewsDone:     0,
            activeYears:     profile.AccountAgeYears,
            publicRepos:     profile.PublicRepos,
            totalStars:      profile.TotalStars,
            p:               scoreParams);

        var dto = new ProfileDto(
            Username:            profile.Username,
            Score:               profile.Score,
            AccountAgeYears:     profile.AccountAgeYears,
            UniqueLanguages:     profile.UniqueLanguages,
            Followers:           profile.Followers,
            PublicRepos:         profile.PublicRepos,
            TotalCommitsLastYear: profile.TotalCommitsLastYear,
            SubstitutionYear:    profile.SubstitutionYear,
            ProfileName:         ProfileClassifier.GetProfileName(profile.Score),
            AgiThreatLevel:      ProfileClassifier.GetAgiThreatLevel(profile.Score),
            FormationTier:       ProfileClassifier.GetFormationTier(profile.AccountAgeYears),
            FormationShape:      ProfileClassifier.GetFormationShape(profile.AccountAgeYears),
            TerrainTier:         ProfileClassifier.GetTerrainTier(profile.UniqueLanguages),
            TerrainName:         ProfileClassifier.GetTerrainName(profile.UniqueLanguages),
            ActivityScore:       score.ActivityScore,
            DiversityScore:      score.DiversityScore,
            CollaborationScore:  score.CollaborationScore,
            SeniorityScore:      score.SeniorityScore,
            OriginalityScore:    score.OriginalityScore,
            ProcessedAt:         profile.ProcessedAt,
            FormationDescription: ProfileClassifier.GetFormationDescription(profile.AccountAgeYears),
            TerrainDescription:  ProfileClassifier.GetTerrainDescription(profile.UniqueLanguages),
            AgiMessage:          ProfileClassifier.GetAgiMessage(profile.Score));

        _cacheService.Set(cacheKey, dto, TimeSpan.FromHours(1));
        return dto;
    }
}
