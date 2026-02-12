using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VibeTravels.Application.Abstractions.Persistence;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthorization();

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            var appDbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAppDbContext));
            if (appDbContextDescriptor is not null)
            {
                services.Remove(appDbContextDescriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("tags-integration-tests"));
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        });
    }
}
