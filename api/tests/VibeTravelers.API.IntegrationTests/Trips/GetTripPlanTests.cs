using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class GetTripPlanTests
{
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly ApiFactory _factory;

    public GetTripPlanTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task GetTripPlan_Returns200_WithPlanAndCorrelationId()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "plan-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);
        await SeedPlanAsync(trip.Id);

        using var client = _factory.CreateClient();
        const string correlationId = "corr-trip-plan-1";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/trips/{trip.Id}/plan");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        Assert.Equal(trip.Id, root.GetProperty("tripId").GetGuid());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("generated", root.GetProperty("status").GetString());
        Assert.Equal("City highlights", root.GetProperty("summary").GetString());

        var items = root.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal("Breakfast", items[0].GetProperty("title").GetString());
        Assert.Equal(1, items[0].GetProperty("dayNumber").GetInt32());
        Assert.Equal(10, items[0].GetProperty("order").GetInt32());
        Assert.Equal("restaurant", items[0].GetProperty("placeType").GetString());
        var firstItemDate = items[0].GetProperty("itemDate").GetDateTimeOffset();
        Assert.Equal(TimeSpan.Zero, firstItemDate.Offset);
        Assert.Equal(new DateTimeOffset(2026, 6, 10, 9, 0, 0, TimeSpan.Zero), firstItemDate);

        Assert.Equal("Museum", items[1].GetProperty("title").GetString());
        Assert.Equal(2, items[1].GetProperty("dayNumber").GetInt32());
        Assert.Equal("attraction", items[1].GetProperty("placeType").GetString());
        var secondItemDate = items[1].GetProperty("itemDate").GetDateTimeOffset();
        Assert.Equal(TimeSpan.Zero, secondItemDate.Offset);
    }

    [Fact]
    public async Task GetTripPlan_Returns404PlanNotFound_WhenTripExistsWithoutPlan()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "plan-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/trips/{trip.Id}/plan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("PLAN_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetTripPlan_Returns404TripNotFound_WhenTripBelongsToDifferentUser()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "plan-owner@example.com");
        await SeedUserAsync(OtherUserId, "plan-other@example.com");
        var foreignTrip = await SeedTripAsync(OtherUserId);
        await SeedPlanAsync(foreignTrip.Id);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/trips/{foreignTrip.Id}/plan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetTripPlan_Returns400_WhenTripIdIsEmptyGuid()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "plan-owner@example.com");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/trips/00000000-0000-0000-0000-000000000000/plan");

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
            title: "Plan trip",
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

    private async Task SeedPlanAsync(Guid tripId)
    {
        var createdAt = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var plan = TripPlan.Create(
            tripId,
            generationJobId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            title: "Trip plan",
            summary: "City highlights",
            createdAt);

        var firstItem = PlanItem.CreateGenerated(
            tripId,
            dayNumber: 1,
            itemDate: new DateTimeOffset(2026, 6, 10, 9, 0, 0, TimeSpan.Zero),
            sortOrder: 10,
            placeType: PlanItemPlaceType.Restaurant,
            title: "Breakfast",
            description: "Cafe stop",
            locationText: "Breakfast",
            createdAt);

        var secondItem = PlanItem.CreateGenerated(
            tripId,
            dayNumber: 2,
            itemDate: new DateTimeOffset(2026, 6, 11, 14, 0, 0, TimeSpan.Zero),
            sortOrder: 20,
            placeType: PlanItemPlaceType.Attraction,
            title: "Museum",
            description: "Old town museum",
            locationText: "Museum",
            createdAt);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.TripPlans.Add(plan);
        db.PlanItems.AddRange(firstItem, secondItem);
        await db.SaveChangesAsync();
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(target, value);
    }
}
