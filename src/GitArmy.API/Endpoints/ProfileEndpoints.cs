using GitArmy.Application.Commands.AnalyseProfile;
using GitArmy.Application.Queries.GetProfile;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;

namespace GitArmy.API.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profiles").WithTags("Profiles");

        group.MapPost("/analyse", async (AnalyseProfileRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AnalyseProfileCommand(request.Username), ct);
            return Results.Ok(result);
        })
        .RequireRateLimiting("analyse")
        .WithName("AnalyseProfile")
        .WithSummary("Analyse a GitHub profile and persist results");

        group.MapGet("/{username}", async (string username, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProfileQuery(username), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetProfile")
        .WithSummary("Get a previously analysed profile by username");

        return app;
    }
}

public record AnalyseProfileRequest(string Username);
