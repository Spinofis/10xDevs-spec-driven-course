using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Jobs;

[Collection("Database")]
public sealed class GetGenerationJobByIdTests
{
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly ApiFactory _factory;

    public GetGenerationJobByIdTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task GetGenerationJobById_Returns200_WithFlatPayloadAndCorrelationId()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "jobs-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);
        var job = await SeedJobAsync(trip.Id, TripsEndpoints.DevelopmentUserId);

        using var client = _factory.CreateClient();
        const string correlationId = "corr-job-get-1";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/generation-jobs/{job.Id}");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        Assert.Equal(job.Id, document.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(job.TripId, document.RootElement.GetProperty("tripId").GetGuid());
        Assert.Equal("queued", document.RootElement.GetProperty("status").GetString());
        Assert.False(document.RootElement.TryGetProperty("job", out _));
        Assert.Equal(2, document.RootElement.GetProperty("attemptNo").GetInt32());
        Assert.Equal("AI_TIMEOUT", document.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("Timed out", document.RootElement.GetProperty("errorMessage").GetString());
        Assert.True(document.RootElement.GetProperty("discarded").GetBoolean());
        Assert.Equal("Superseded", document.RootElement.GetProperty("discardReason").GetString());
    }

    [Fact]
    public async Task GetGenerationJobById_Returns404_WhenJobDoesNotExist()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "jobs-owner@example.com");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/generation-jobs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("JOB_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetGenerationJobById_Returns404_WhenJobBelongsToDifferentUser()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "jobs-owner@example.com");
        await SeedUserAsync(OtherUserId, "jobs-other@example.com");
        var trip = await SeedTripAsync(OtherUserId);
        var foreignJob = await SeedJobAsync(trip.Id, OtherUserId);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/generation-jobs/{foreignJob.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("JOB_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
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

    private async Task SeedUserAsync(Guid id, string email)
    {
        var result = User.Create(email, "test-password-hash");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var user = result.Value!;
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task<Trip> SeedTripAsync(Guid userId)
    {
        var createResult = Trip.Create(
            userId,
            title: "Get job trip",
            placeText: "Lisbon",
            noteText: "Ocean and city",
            dateFrom: new DateOnly(2026, 6, 10),
            dateTo: new DateOnly(2026, 6, 16),
            stayLengthMinDays: 2,
            stayLengthMaxDays: 7,
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

    private async Task<AiGenerationJob> SeedJobAsync(Guid tripId, Guid userId)
    {
        var jobResult = AiGenerationJob.CreatePending(
            tripId,
            userId,
            """{"tripId":"123"}""",
            "hash",
            DateTimeOffset.UtcNow);

        Assert.True(jobResult.IsSuccess);
        Assert.NotNull(jobResult.Value);

        var job = jobResult.Value!;
        typeof(AiGenerationJob).GetProperty(nameof(AiGenerationJob.AttemptNo))!.SetValue(job, 2);
        typeof(AiGenerationJob).GetProperty(nameof(AiGenerationJob.ErrorCode))!.SetValue(job, "AI_TIMEOUT");
        typeof(AiGenerationJob).GetProperty(nameof(AiGenerationJob.ErrorMessage))!.SetValue(job, "Timed out");
        typeof(AiGenerationJob).GetProperty(nameof(AiGenerationJob.Discarded))!.SetValue(job, true);
        typeof(AiGenerationJob).GetProperty(nameof(AiGenerationJob.DiscardReason))!.SetValue(job, "Superseded");

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AiGenerationJobs.Add(job);
        await db.SaveChangesAsync();

        return job;
    }
}
