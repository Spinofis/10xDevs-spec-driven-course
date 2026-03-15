using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class QueueGenerationJobTests
{
    private readonly ApiFactory _factory;

    public QueueGenerationJobTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task QueueGenerationJob_Returns202_AndCreatesJobAndSnapshot()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync(stayMin: 2, stayMax: 7);

        using var client = _factory.CreateClient();
        const string correlationId = "corr-job-create-1";
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/trips/{trip.Id}/generation-jobs");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        Assert.Equal("queued", document.RootElement.GetProperty("job").GetProperty("status").GetString());
        Assert.Equal(trip.Id, document.RootElement.GetProperty("job").GetProperty("tripId").GetGuid());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await db.AiGenerationJobs.CountAsync(x => x.TripId == trip.Id));
    }

    [Fact]
    public async Task QueueGenerationJob_Returns404_WhenTripDoesNotExist()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        var response = await client.PostAsync($"/trips/{Guid.NewGuid()}/generation-jobs", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task QueueGenerationJob_Returns400_WhenGenerationRequirementsAreNotMet()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync(stayMin: 1, stayMax: 1);

        using var client = _factory.CreateClient();
        var response = await client.PostAsync($"/trips/{trip.Id}/generation-jobs", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("GENERATION_REQUIREMENTS_NOT_MET", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task QueueGenerationJob_Returns409_WhenActiveJobAlreadyExists()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync(stayMin: 2, stayMax: 7);
        await SeedActiveJobAsync(trip.Id);

        using var client = _factory.CreateClient();
        var response = await client.PostAsync($"/trips/{trip.Id}/generation-jobs", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("JOB_ALREADY_ACTIVE", document.RootElement.GetProperty("title").GetString());
    }

    private async Task ResetDatabaseAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.AiGenerationJobs.RemoveRange(db.AiGenerationJobs);
        db.TripTags.RemoveRange(db.TripTags);
        db.Trips.RemoveRange(db.Trips);
        db.Tags.RemoveRange(db.Tags);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }

    private async Task SeedDevelopmentUserAsync()
    {
        var result = User.Create("queue-jobs-tests@example.com", "test-password-hash");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var user = result.Value!;
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, TripsEndpoints.DevelopmentUserId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task<Trip> SeedTripAsync(int stayMin, int stayMax)
    {
        var createResult = Trip.Create(
            TripsEndpoints.DevelopmentUserId,
            title: "Queue job trip",
            placeText: "Barcelona",
            noteText: "Architecture and food",
            dateFrom: new DateOnly(2026, 6, 10),
            dateTo: new DateOnly(2026, 6, 16),
            stayLengthMinDays: stayMin,
            stayLengthMaxDays: stayMax,
            peopleCount: 2,
            budgetLevel: "Medium",
            pace: "Normal",
            hasAnyTags: false);

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(createResult.Value);

        var trip = createResult.Value!;

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        return trip;
    }

    private async Task SeedActiveJobAsync(Guid tripId)
    {
        var jobResult = AiGenerationJob.CreatePending(
            tripId,
            """{"tripId":"123"}""",
            "hash",
            DateTimeOffset.UtcNow);

        Assert.True(jobResult.IsSuccess);
        Assert.NotNull(jobResult.Value);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AiGenerationJobs.Add(jobResult.Value!);
        await db.SaveChangesAsync();
    }
}
