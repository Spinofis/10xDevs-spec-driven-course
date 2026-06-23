using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DatabaseName = "integration_tests";
    private const string Username = "it_user";
    private const string Password = "it_pass";

    private readonly IContainer _postgreSqlContainer = new ContainerBuilder()
        .WithImage("postgres:16-alpine")
        .WithPortBinding(5432, true)
        .WithEnvironment("POSTGRES_DB", DatabaseName)
        .WithEnvironment("POSTGRES_USER", Username)
        .WithEnvironment("POSTGRES_PASSWORD", Password)
        .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public ApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();

        var connectionString =
            $"Host={_postgreSqlContainer.Hostname};Port={_postgreSqlContainer.GetMappedPublicPort(5432)};Database={DatabaseName};Username={Username};Password={Password}";
        Factory = new ApiFactory(connectionString);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            Factory.Dispose();
        }

        await _postgreSqlContainer.DisposeAsync();
    }
}
