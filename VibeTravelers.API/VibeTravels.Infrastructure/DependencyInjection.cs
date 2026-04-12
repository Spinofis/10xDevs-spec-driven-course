using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VibeTravels.Application.Abstractions.Integrations;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Infrastructure.Integrations.OpenAI;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravels.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));

        services.AddHttpClient<IOpenAiClient, OpenAiClient>((sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<OpenAiOptions>>().CurrentValue;
            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "https://api.openai.com"
                : options.BaseUrl;

            httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
        });

        return services;
    }
}
