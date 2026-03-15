using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Jobs;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class ListTripGenerationJobsTests
{
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly ApiFactory _factory;

    public ListTripGenerationJobsTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task ListTripGenerationJobs_Returns200_WithItemsAndCorrelationId()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "jobs-list-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);

        var newest = CreateJob(trip.Id, TripsEndpoints.DevelopmentUserId);
        SetProperty(newest, nameof(AiGenerationJob.Id), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"));
        SetProperty(newest, nameof(AiGenerationJob.RequestedAt), new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));

        var older = CreateJob(trip.Id, TripsEndpoints.DevelopmentUserId);
        SetProperty(older, nameof(AiGenerationJob.Id), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"));
        SetProperty(older, nameof(AiGenerationJob.RequestedAt), new DateTimeOffset(2026, 7, 1, 11, 0, 0, TimeSpan.Zero));
        SetProperty(older, nameof(AiGenerationJob.Status), AiGenerationJobStatus.Succeeded);

        await SeedJobsAsync(newest, older);

        using var client = _factory.CreateClient();
        const string correlationId = "corr-trip-jobs-1";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/trips/{trip.Id}/generation-jobs?limit=1");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var items = document.RootElement.GetProperty("items");
        Assert.Single(items.EnumerateArray());
        Assert.Equal(newest.Id, items[0].GetProperty("id").GetGuid());
        Assert.Equal("queued", items[0].GetProperty("status").GetString());

        var nextCursor = document.RootElement.GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(nextCursor));
    }

    [Fact]
    public async Task ListTripGenerationJobs_PaginatesStably_WhenRequestedAtIsEqual()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "jobs-list-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);

        var sameTimestamp = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        var firstByIdDesc = CreateJob(trip.Id, TripsEndpoints.DevelopmentUserId);
        SetProperty(firstByIdDesc, nameof(AiGenerationJob.Id), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"));
        SetProperty(firstByIdDesc, nameof(AiGenerationJob.RequestedAt), sameTimestamp);
        SetProperty(firstByIdDesc, nameof(AiGenerationJob.Status), AiGenerationJobStatus.Succeeded);

        var secondByIdDesc = CreateJob(trip.Id, TripsEndpoints.DevelopmentUserId);
        SetProperty(secondByIdDesc, nameof(AiGenerationJob.Id), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"));
        SetProperty(secondByIdDesc, nameof(AiGenerationJob.RequestedAt), sameTimestamp);
        SetProperty(secondByIdDesc, nameof(AiGenerationJob.Status), AiGenerationJobStatus.Failed);

        var thirdByIdDesc = CreateJob(trip.Id, TripsEndpoints.DevelopmentUserId);
        SetProperty(thirdByIdDesc, nameof(AiGenerationJob.Id), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"));
        SetProperty(thirdByIdDesc, nameof(AiGenerationJob.RequestedAt), sameTimestamp);
        SetProperty(thirdByIdDesc, nameof(AiGenerationJob.Status), AiGenerationJobStatus.Canceled);

        await SeedJobsAsync(firstByIdDesc, secondByIdDesc, thirdByIdDesc);

        using var client = _factory.CreateClient();

        var first = await client.GetAsync($"/trips/{trip.Id}/generation-jobs?limit=2");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        await using var firstStream = await first.Content.ReadAsStreamAsync();
        using var firstDoc = await JsonDocument.ParseAsync(firstStream);
        var firstItems = firstDoc.RootElement.GetProperty("items");

        Assert.Equal(2, firstItems.GetArrayLength());
        Assert.Equal(firstByIdDesc.Id, firstItems[0].GetProperty("id").GetGuid());
        Assert.Equal(secondByIdDesc.Id, firstItems[1].GetProperty("id").GetGuid());

        var nextCursor = firstDoc.RootElement.GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(nextCursor));

        var second = await client.GetAsync(
            $"/trips/{trip.Id}/generation-jobs?limit=2&cursor={Uri.EscapeDataString(nextCursor!)}");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        await using var secondStream = await second.Content.ReadAsStreamAsync();
        using var secondDoc = await JsonDocument.ParseAsync(secondStream);
        var secondItems = secondDoc.RootElement.GetProperty("items");

        Assert.Single(secondItems.EnumerateArray());
        Assert.Equal(thirdByIdDesc.Id, secondItems[0].GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, secondDoc.RootElement.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public async Task ListTripGenerationJobs_Returns404_WhenTripDoesNotExist()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "jobs-list-owner@example.com");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/trips/{Guid.NewGuid()}/generation-jobs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ListTripGenerationJobs_Returns404_WhenTripBelongsToDifferentUser()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "jobs-list-owner@example.com");
        await SeedUserAsync(OtherUserId, "jobs-list-other@example.com");
        var foreignTrip = await SeedTripAsync(OtherUserId);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/trips/{foreignTrip.Id}/generation-jobs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ListTripGenerationJobs_Returns400_WhenCursorIsInvalid()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "jobs-list-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/trips/{trip.Id}/generation-jobs?cursor=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("title").GetString());
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
            title: "Jobs list trip",
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

    private async Task SeedJobsAsync(params AiGenerationJob[] jobs)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AiGenerationJobs.AddRange(jobs);
        await db.SaveChangesAsync();
    }

    private static AiGenerationJob CreateJob(Guid tripId, Guid userId)
    {
        var jobResult = AiGenerationJob.CreatePending(
            tripId,
            userId,
            """{"tripId":"123"}""",
            "hash",
            DateTimeOffset.UtcNow);

        Assert.True(jobResult.IsSuccess);
        Assert.NotNull(jobResult.Value);
        return jobResult.Value!;
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(target, value);
    }
}
