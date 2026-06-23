using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Plans;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class PutTripPlanTests
{
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly ApiFactory _factory;

    public PutTripPlanTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task PutTripPlan_Returns200_AndPersistsManualPlan()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "plan-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);
        await SeedGeneratedPlanAsync(trip.Id);

        var payload = """
        {
          "summary": "Manual itinerary",
          "items": [
            {
              "id": "44444444-4444-4444-4444-444444444444",
              "dayNumber": 1,
              "itemDate": "2026-06-10T09:15:00Z",
              "order": 10,
              "title": "Breakfast",
              "description": "Cafe stop",
              "locationText": "Old Town",
              "createdAt": "2026-06-09T10:00:00Z",
              "updatedAt": "2026-06-09T11:00:00Z",
              "placeType": "restaurant"
            }
          ]
        }
        """;

        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/trips/{trip.Id}/plan");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Correlation-Id", "corr-put-plan-1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("corr-put-plan-1", response.Headers.GetValues("X-Correlation-Id").Single());

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        Assert.Equal("saved", root.GetProperty("status").GetString());
        Assert.Equal(2, root.GetProperty("version").GetInt32());
        Assert.Equal("Manual itinerary", root.GetProperty("summary").GetString());
        var itemDate = root.GetProperty("items")[0].GetProperty("itemDate").GetDateTimeOffset();
        Assert.Equal(TimeSpan.Zero, itemDate.Offset);
        Assert.Equal(new DateTimeOffset(2026, 6, 10, 9, 15, 0, TimeSpan.Zero), itemDate);
    }

    [Fact]
    public async Task PutTripPlan_Returns400_WhenItemsAreEmpty()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "plan-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);
        await SeedGeneratedPlanAsync(trip.Id);

        const string payload = """{ "summary": "Manual itinerary", "items": [] }""";
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/trips/{trip.Id}/plan");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PutTripPlan_Returns404TripNotFound_WhenTripBelongsToDifferentUser()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "plan-owner@example.com");
        await SeedUserAsync(OtherUserId, "plan-other@example.com");
        var foreignTrip = await SeedTripAsync(OtherUserId);
        await SeedGeneratedPlanAsync(foreignTrip.Id);

        var payload = """
        {
          "summary": "Manual itinerary",
          "items": [
            {
              "id": "44444444-4444-4444-4444-444444444444",
              "dayNumber": 1,
              "itemDate": "2026-06-10T09:15:00Z",
              "order": 10,
              "title": "Breakfast",
              "description": "Cafe stop",
              "locationText": "Old Town",
              "createdAt": "2026-06-09T10:00:00Z",
              "updatedAt": "2026-06-09T11:00:00Z",
              "placeType": "restaurant"
            }
          ]
        }
        """;
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/trips/{foreignTrip.Id}/plan");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PutTripPlan_Returns404PlanNotFound_WhenTripExistsWithoutPlan()
    {
        await ResetDatabaseAsync();
        await SeedUserAsync(TripsEndpoints.DevelopmentUserId, "plan-owner@example.com");
        var trip = await SeedTripAsync(TripsEndpoints.DevelopmentUserId);

        var payload = """
        {
          "summary": "Manual itinerary",
          "items": [
            {
              "id": "44444444-4444-4444-4444-444444444444",
              "dayNumber": 1,
              "itemDate": "2026-06-10T09:15:00Z",
              "order": 10,
              "title": "Breakfast",
              "description": "Cafe stop",
              "locationText": "Old Town",
              "createdAt": "2026-06-09T10:00:00Z",
              "updatedAt": "2026-06-09T11:00:00Z",
              "placeType": "restaurant"
            }
          ]
        }
        """;

        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/trips/{trip.Id}/plan");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("PLAN_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
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

    private async Task SeedGeneratedPlanAsync(Guid tripId)
    {
        var createdAt = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var plan = TripPlan.Create(
            tripId,
            generationJobId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            title: "Trip plan",
            summary: "City highlights",
            createdAt);

        var item = PlanItem.CreateGenerated(
            tripId,
            dayNumber: 1,
            itemDate: new DateTimeOffset(2026, 6, 10, 9, 0, 0, TimeSpan.Zero),
            sortOrder: 10,
            placeType: PlanItemPlaceType.Restaurant,
            title: "Breakfast",
            description: "Cafe stop",
            locationText: "Old Town",
            createdAt);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.TripPlans.Add(plan);
        db.PlanItems.Add(item);
        await db.SaveChangesAsync();
    }

    private static void SetProperty<T>(T target, string propertyName, object? value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(target, value);
    }
}
