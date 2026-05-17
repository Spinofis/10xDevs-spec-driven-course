using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Application.Features.Trips.Services;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class SaveTripPlanTests
{
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly ApiFactory _factory;

    public SaveTripPlanTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task SaveTripPlan_Returns200_AndMarksGeneratedPlanAsSaved()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "save-plan-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);
        await SeedGeneratedPlanWithMatchingJobAsync(trip);

        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/trips/{trip.Id}/plan/save");
        request.Headers.Add("X-Correlation-Id", "corr-save-plan-1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("corr-save-plan-1", response.Headers.GetValues("X-Correlation-Id").Single());

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        Assert.Equal(trip.Id, root.GetProperty("tripId").GetGuid());
        Assert.Equal("saved", root.GetProperty("status").GetString());
        Assert.Equal(2, root.GetProperty("version").GetInt32());
        Assert.NotEqual(default, root.GetProperty("savedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task SaveTripPlan_Returns404TripNotFound_WhenTripBelongsToDifferentUser()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "save-plan-owner@example.com");
        await SeedUserAsync(OtherUserId, "save-plan-other@example.com");
        var foreignTrip = await SeedTripAsync(OtherUserId);
        await SeedGeneratedPlanWithMatchingJobAsync(foreignTrip);

        using var client = _factory.CreateClient();
        var response = await client.PostAsync($"/trips/{foreignTrip.Id}/plan/save", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task SaveTripPlan_Returns404PlanNotFound_WhenTripExistsWithoutPlan()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "save-plan-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);

        using var client = _factory.CreateClient();
        var response = await client.PostAsync($"/trips/{trip.Id}/plan/save", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("PLAN_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task SaveTripPlan_Returns409_WhenTripInputChangedSinceGeneration()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "save-plan-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);
        await SeedGeneratedPlanWithMismatchedJobAsync(trip);

        using var client = _factory.CreateClient();
        var response = await client.PostAsync($"/trips/{trip.Id}/plan/save", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("INPUT_CHANGED_SINCE_GENERATION", document.RootElement.GetProperty("title").GetString());
    }

    private async Task ResetDatabaseAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.PlanItems.RemoveRange(db.PlanItems);
        db.TripPlans.RemoveRange(db.TripPlans);
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
        SetProperty(user, nameof(User.Id), id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task<Trip> SeedTripAsync(Guid userId)
    {
        var createResult = Trip.Create(
            userId,
            title: "Save plan trip",
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

    private async Task SeedGeneratedPlanWithMatchingJobAsync(Trip trip)
    {
        var fingerprint = new TripInputFingerprintService().Build(trip, trip.UserId);
        Assert.True(fingerprint.IsSuccess);
        Assert.NotNull(fingerprint.Value);

        await SeedGeneratedPlanAsync(
            trip.Id,
            trip.UserId,
            fingerprint.Value!.PayloadJson,
            fingerprint.Value.Hash);
    }

    private async Task SeedGeneratedPlanWithMismatchedJobAsync(Trip trip)
    {
        await SeedGeneratedPlanAsync(
            trip.Id,
            trip.UserId,
            """{"tripId":"123"}""",
            "different-hash");
    }

    private async Task SeedGeneratedPlanAsync(
        Guid tripId,
        Guid userId,
        string inputSnapshot,
        string inputHash)
    {
        var createdAt = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var jobResult = AiGenerationJob.CreatePending(
            tripId,
            userId,
            inputSnapshot,
            inputHash,
            createdAt);

        Assert.True(jobResult.IsSuccess);
        Assert.NotNull(jobResult.Value);

        var plan = TripPlan.Create(
            tripId,
            generationJobId: jobResult.Value!.Id,
            title: "Trip plan",
            summary: "City highlights",
            createdAt);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AiGenerationJobs.Add(jobResult.Value!);
        db.TripPlans.Add(plan);
        await db.SaveChangesAsync();
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(target, value);
    }
}
