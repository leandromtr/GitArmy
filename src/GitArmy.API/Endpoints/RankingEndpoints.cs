using GitArmy.Application.Queries.GetRanking;
using MediatR;

namespace GitArmy.API.Endpoints;

public static class RankingEndpoints
{
    public static IEndpointRouteBuilder MapRankingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ranking", async (ISender sender, CancellationToken ct, int count = 20) =>
        {
            var result = await sender.Send(new GetRankingQuery(Math.Clamp(count, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithTags("Ranking")
        .WithName("GetRanking")
        .WithSummary("Get top N profiles ordered by score");

        return app;
    }
}
