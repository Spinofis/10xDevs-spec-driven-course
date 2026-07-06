using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class GetTripByIdTests
{
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly ApiFactory _factory;

    public GetTripByIdTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task GetTripById_Returns200_WithTripTagsAndCorrelationId()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "trip-owner@example.com");

        var tagA = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "beach", "Beach");
        var tagB = CreateTag(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "museum", "Museum");
        await SeedTagsAsync(tagA, tagB);

        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId, hasAnyTags: true);
        await SeedTripTagsAsync(trip.Id, (tagB.Id, 2), (tagA.Id, 1));

        using var client = _factory.CreateClient();
        const string correlationId = "corr-get-trip-1";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/trips/{trip.Id}");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        var tripJson = root.GetProperty("trip");
        Assert.Equal(trip.Id, tripJson.GetProperty("id").GetGuid());
        Assert.Equal("Trip title", tripJson.GetProperty("title").GetString());
        Assert.Equal("Paris", tripJson.GetProperty("placeText").GetString());
        Assert.Equal("Trip note", tripJson.GetProperty("noteText").GetString());
        Assert.Equal("medium", tripJson.GetProperty("budgetLevel").GetString());
        Assert.Equal("normal", tripJson.GetProperty("pace").GetString());
        Assert.False(tripJson.GetProperty("hasGeneratedPlan").GetBoolean());

        var tags = root.GetProperty("tags");
        Assert.Equal(2, tags.GetArrayLength());
        Assert.Equal("beach", tags[0].GetProperty("tag").GetProperty("code").GetString());
        Assert.True(tags[0].GetProperty("tag").TryGetProperty("createdAt", out _));
        Assert.Equal(1, tags[0].GetProperty("order").GetInt32());
        Assert.Equal("museum", tags[1].GetProperty("tag").GetProperty("code").GetString());
        Assert.Equal(2, tags[1].GetProperty("order").GetInt32());
    }

    [Fact]
    public async Task GetTripById_Returns404_WhenTripDoesNotExist()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "trip-owner@example.com");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/trips/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetTripById_Returns404_WhenTripBelongsToDifferentUser()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "trip-owner@example.com");
        await SeedUserAsync(OtherUserId, "trip-other@example.com");
        var foreignTrip = await SeedTripAsync(OtherUserId);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/trips/{foreignTrip.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetTripById_Returns404_WhenTripIsSoftDeleted()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "trip-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storedTrip = await db.Trips.FindAsync(trip.Id);
            Assert.NotNull(storedTrip);
            var deleteResult = storedTrip!.SoftDelete(DateTimeOffset.UtcNow);
            Assert.True(deleteResult.IsSuccess);
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/trips/{trip.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetTripById_Returns400_WhenTripIdIsEmptyGuid()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "trip-owner@example.com");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/trips/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("title").GetString());
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
        db.UserPreferenceTags.RemoveRange(db.UserPreferenceTags);
        db.UserProfiles.RemoveRange(db.UserProfiles);
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

    private async Task<Trip> SeedTripAsync(Guid userId, bool hasAnyTags = false)
    {
        var createResult = Trip.Create(
            userId,
            title: "Trip title",
            placeText: "Paris",
            noteText: "Trip note",
            dateFrom: new DateOnly(2026, 5, 1),
            dateTo: new DateOnly(2026, 5, 7),
            stayLengthMinDays: 3,
            stayLengthMaxDays: 7,
            peopleCount: 2,
            budgetLevel: "Medium",
            pace: "Normal",
            hasAnyTags: hasAnyTags);

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(createResult.Value);

        var trip = createResult.Value!;
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        return trip;
    }

    private async Task SeedTagsAsync(params Tag[] tags)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tags.AddRange(tags);
        await db.SaveChangesAsync();
    }

    private async Task SeedTripTagsAsync(Guid tripId, params (Guid TagId, int Order)[] tags)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tripTags = tags
            .Select(t => TripTag.Create(tripId, t.TagId, t.Order).Value!)
            .ToArray();

        db.TripTags.AddRange(tripTags);
        await db.SaveChangesAsync();
    }

    private static Tag CreateTag(Guid id, string code, string displayName)
    {
        var tag = (Tag)Activator.CreateInstance(typeof(Tag), nonPublic: true)!;
        SetProperty(tag, nameof(Tag.Id), id);
        SetProperty(tag, nameof(Tag.Code), code);
        SetProperty(tag, nameof(Tag.DisplayName), displayName);
        SetProperty(tag, nameof(Tag.CreatedAt), DateTimeOffset.UtcNow);
        return tag;
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(target, value);
    }
}
