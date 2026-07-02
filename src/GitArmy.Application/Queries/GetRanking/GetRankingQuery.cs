using GitArmy.Application.DTOs;
using MediatR;

namespace GitArmy.Application.Queries.GetRanking;

public record GetRankingQuery(int Count = 20) : IRequest<IReadOnlyList<RankingEntryDto>>;
