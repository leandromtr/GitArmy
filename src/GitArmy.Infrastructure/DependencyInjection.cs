using GitArmy.Application.Interfaces;
using GitArmy.Domain.Interfaces;
using GitArmy.Infrastructure.Cache;
using GitArmy.Infrastructure.GitHub;
using GitArmy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GitArmy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IProfileRepository, ProfileRepository>();

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddHttpClient<IGitHubClient, GitHubApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "GitArmy/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var token = configuration["GitHub:Token"];
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        });

        return services;
    }
}
