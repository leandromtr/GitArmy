using GitArmy.API.Endpoints;
using GitArmy.API.Handlers;
using GitArmy.API.Middleware;
using GitArmy.Application;
using GitArmy.Application.Configuration;
using GitArmy.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ScoringSettings>(
    builder.Configuration.GetSection(ScoringSettings.Section));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("analyse", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromHours(1);
        limiterOptions.SegmentsPerWindow = 6;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.EndpointPathPrefix = "/scalar/{documentName}");
}

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.MapProfileEndpoints();
app.MapRankingEndpoints();
app.MapScoringRulesEndpoints();

app.Run();

public partial class Program { }
