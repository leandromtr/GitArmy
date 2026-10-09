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
                    commitSaturate   = s.CommitSaturate,
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
                    domainSaturateCount = s.DomainSaturateCount,
                    langMinBytes  = s.LangMinBytes,
                },
                leadership = new
                {
                    maxPts          = 25,
                    followersSaturate = s.FollowersSaturate,
                    followersCap    = s.FollowersCap,
                    prCreatedPts    = s.PrCreatedPts,
                    prMergedPts     = s.PrMergedPts,
                    reviewPts       = s.ReviewPts,
                    colaborSaturate = s.ColaborSaturate,
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
                    repoSaturate  = s.RepoSaturate,
                    repoCap       = s.RepoCap,
                    repoMinCodeBytes = s.RepoMinCodeBytes,
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
