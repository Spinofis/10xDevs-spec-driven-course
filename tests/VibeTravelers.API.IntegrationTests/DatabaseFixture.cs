namespace VibeTravelers.API.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    public ApiFactory Factory { get; } = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
}
