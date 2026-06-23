using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class ListTripsTests
{
    private readonly ApiFactory _factory;

    public ListTripsTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task ListTrips_Returns200AndEmpty_WhenNoTrips()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/trips");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        Assert.Equal(0, document.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public async Task ListTrips_FiltersByHasPlan()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        var tripA = CreateTrip(
            id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
            title: "Trip A",
            placeText: "Rome",
            noteText: null,
            createdAt: new DateTimeOffset(2026, 02, 01, 10, 00, 00, TimeSpan.Zero),
            hasGeneratedPlan: false,
            generatedAt: null);

        var tripB = CreateTrip(
            id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            title: "Trip B",
            placeText: "Paris",
            noteText: null,
            createdAt: new DateTimeOffset(2026, 02, 01, 11, 00, 00, TimeSpan.Zero),
            hasGeneratedPlan: true,
            generatedAt: new DateTimeOffset(2026, 02, 02, 12, 00, 00, TimeSpan.Zero));

        await SeedTripsAsync(tripA, tripB);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/trips?hasPlan=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var items = document.RootElement.GetProperty("items");
        Assert.Single(items.EnumerateArray());
        Assert.Equal("Trip B", items[0].GetProperty("title").GetString());
        Assert.True(items[0].GetProperty("hasGeneratedPlan").GetBoolean());
    }

    [Fact]
    public async Task ListTrips_FiltersByQuery_CaseInsensitive()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        var tripA = CreateTrip(
            id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
            title: "Trip to Rome",
            placeText: "Rome, Italy",
            noteText: "Food and museums",
            createdAt: new DateTimeOffset(2026, 02, 03, 10, 00, 00, TimeSpan.Zero),
            hasGeneratedPlan: false,
            generatedAt: null);

        var tripB = CreateTrip(
            id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            title: "Trip to Paris",
            placeText: "Paris, France",
            noteText: null,
            createdAt: new DateTimeOffset(2026, 02, 03, 11, 00, 00, TimeSpan.Zero),
            hasGeneratedPlan: false,
            generatedAt: null);

        await SeedTripsAsync(tripA, tripB);

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/trips?q=ROME");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var items = document.RootElement.GetProperty("items");
        Assert.Single(items.EnumerateArray());
        Assert.Equal("Trip to Rome", items[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task ListTrips_PaginatesWithCursor_SortByCreatedAtDesc()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        var trip1 = CreateTrip(
            id: Guid.Parse("11111111-2222-3333-4444-555555555551"),
            title: "Trip 1",
            placeText: "X",
            noteText: null,
            createdAt: new DateTimeOffset(2026, 02, 10, 10, 00, 00, TimeSpan.Zero),
            hasGeneratedPlan: false,
            generatedAt: null);

        var trip2 = CreateTrip(
            id: Guid.Parse("11111111-2222-3333-4444-555555555552"),
            title: "Trip 2",
            placeText: "Y",
            noteText: null,
            createdAt: new DateTimeOffset(2026, 02, 10, 09, 00, 00, TimeSpan.Zero),
            hasGeneratedPlan: false,
            generatedAt: null);

        var trip3 = CreateTrip(
            id: Guid.Parse("11111111-2222-3333-4444-555555555553"),
            title: "Trip 3",
            placeText: "Z",
            noteText: null,
            createdAt: new DateTimeOffset(2026, 02, 10, 08, 00, 00, TimeSpan.Zero),
            hasGeneratedPlan: false,
            generatedAt: null);

        await SeedTripsAsync(trip1, trip2, trip3);

        using var client = _factory.CreateClient();

        var first = await client.GetAsync("/trips?limit=2&sort=-createdAt");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        await using var firstStream = await first.Content.ReadAsStreamAsync();
        using var firstDoc = await JsonDocument.ParseAsync(firstStream);

        var firstItems = firstDoc.RootElement.GetProperty("items");
        Assert.Equal(2, firstItems.GetArrayLength());
        Assert.Equal("Trip 1", firstItems[0].GetProperty("title").GetString());
        Assert.Equal("Trip 2", firstItems[1].GetProperty("title").GetString());

        var nextCursor = firstDoc.RootElement.GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(nextCursor));

        var second = await client.GetAsync($"/trips?limit=2&sort=-createdAt&cursor={Uri.EscapeDataString(nextCursor!)}");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        await using var secondStream = await second.Content.ReadAsStreamAsync();
        using var secondDoc = await JsonDocument.ParseAsync(secondStream);

        var secondItems = secondDoc.RootElement.GetProperty("items");
        Assert.Single(secondItems.EnumerateArray());
        Assert.Equal("Trip 3", secondItems[0].GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.Null, secondDoc.RootElement.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public async Task ListTrips_Returns400_WhenSortIsInvalid()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/trips?sort=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ListTrips_Returns400_WhenCursorIsInvalid()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/trips?cursor=not-a-valid-cursor");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("title").GetString());
    }

    private async Task ResetDatabaseAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.TripTags.RemoveRange(db.TripTags);
        db.Trips.RemoveRange(db.Trips);
        db.Tags.RemoveRange(db.Tags);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }

    private async Task SeedDevelopmentUserAsync()
    {
        var result = User.Create("list-trips-tests@example.com", "test-password-hash");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var user = result.Value!;
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, TripsEndpoints.DevelopmentUserId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task SeedTripsAsync(params Trip[] trips)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Trips.AddRange(trips);
        await db.SaveChangesAsync();
    }

    private static Trip CreateTrip(
        Guid id,
        string title,
        string? placeText,
        string? noteText,
        DateTimeOffset createdAt,
        bool hasGeneratedPlan,
        DateTimeOffset? generatedAt)
    {
        var tripResult = Trip.Create(
            TripsEndpoints.DevelopmentUserId,
            title,
            placeText,
            noteText,
            dateFrom: new DateOnly(2026, 05, 01),
            dateTo: new DateOnly(2026, 05, 07),
            stayLengthMinDays: 5,
            stayLengthMaxDays: 7,
            peopleCount: 2,
            budgetLevel: null,
            pace: null,
            hasAnyTags: false);

        Assert.True(tripResult.IsSuccess);
        Assert.NotNull(tripResult.Value);

        var trip = tripResult.Value!;
        typeof(Trip).GetProperty(nameof(Trip.Id))!.SetValue(trip, id);
        typeof(Trip).GetProperty(nameof(Trip.CreatedAt))!.SetValue(trip, createdAt);
        typeof(Trip).GetProperty(nameof(Trip.UpdatedAt))!.SetValue(trip, createdAt);
        typeof(Trip).GetProperty(nameof(Trip.HasGeneratedPlan))!.SetValue(trip, hasGeneratedPlan);
        typeof(Trip).GetProperty(nameof(Trip.GeneratedAt))!.SetValue(trip, generatedAt);

        return trip;
    }
}

