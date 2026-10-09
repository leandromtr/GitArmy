using GitArmy.Application.Common;
using GitArmy.Application.DTOs;
using GitArmy.Domain.Interfaces;
using GitArmy.Domain.ValueObjects;
using MediatR;

namespace GitArmy.Application.Queries.GetRanking;

internal sealed class GetRankingHandler : IRequestHandler<GetRankingQuery, IReadOnlyList<RankingEntryDto>>
{
    private readonly IProfileRepository _profileRepository;

    public GetRankingHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<IReadOnlyList<RankingEntryDto>> Handle(GetRankingQuery request, CancellationToken cancellationToken)
    {
        var profiles = await _profileRepository.GetTopRankingAsync(request.Count, cancellationToken);

        return profiles
            .Select((p, i) => new RankingEntryDto(
                Position: i + 1,
                Username: p.Username,
                ProfileName: ProfileClassifier.GetProfileName(p.Score),
                // Calculado a partir do score (e não do valor guardado) para refletir sempre a linha do tempo atual.
                SubstitutionYear: Score.SubstitutionYearFor(p.Score),
                ProcessedAt: p.ProcessedAt))
            .ToList();
    }
}
