using GitArmy.Application.Configuration;
using GitArmy.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace GitArmy.API.Endpoints;

public static class ScoringRulesEndpoints
{
    public static IEndpointRouteBuilder MapScoringRulesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/scoring-rules", (IOptions<ScoringSettings> opts) =>
        {
            var s = opts.Value;
            return Results.Ok(new
            {
                activity = new
                {
                    maxPts           = 25,
                    commitMultiplier = s.CommitMultiplier,
                    commitCap        = s.CommitCap,
                    consistencyWeeks = s.ConsistencyWeeks,
                    consistencyCap   = s.ConsistencyCap,
                },
                diversity = new
                {
                    maxPts        = 20,
                    langPointEach = s.LangPointEach,
                    langCap       = s.LangCap,
                    domainCap     = s.DomainCap,
                    domainWeights = s.DomainWeights,
                },
                leadership = new
                {
                    maxPts          = 25,
                    followersSaturate = s.FollowersSaturate,
                    followersCap    = s.FollowersCap,
                    prCreatedPts    = s.PrCreatedPts,
                    prMergedPts     = s.PrMergedPts,
                    reviewPts       = s.ReviewPts,
                    colaborCap      = s.ColaborCap,
                },
                seniority = new
                {
                    maxPts      = 15,
                    activeYearPts = s.ActiveYearPts,
                    cap         = s.AntiguidadeCap,
                    minContributions = s.ActiveYearMinContributions,
                },
                originality = new
                {
                    maxPts        = 15,
                    repoPts       = s.RepoPts,
                    repoCap       = s.RepoCap,
                    starsSaturate = s.StarsSaturate,
                    starsCap      = s.StarsCap,
                },
                timeline = new
                {
                    baseYear        = Score.SubstitutionBaseYear,
                    spanYears       = Score.SubstitutionSpanYears,
                    singularityYear = Score.SingularityYear,
                },
            });
        })
        .WithTags("Rules")
        .WithName("GetScoringRules")
        .WithSummary("Get current scoring rules configuration")
        .AllowAnonymous();

        return app;
    }
}
