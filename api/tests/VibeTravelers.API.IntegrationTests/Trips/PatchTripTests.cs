using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class PatchTripTests
{
    private readonly ApiFactory _factory;

    public PatchTripTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task PatchTrip_Returns200_WhenSingleFieldIsUpdated()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync(title: "Original title", noteText: "Old note");

        using var client = _factory.CreateClient();
        var json = """{ "title": "Updated title" }""";

        var response = await client.PatchAsync($"/trips/{trip.Id}", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        Assert.Equal("Updated title", document.RootElement.GetProperty("trip").GetProperty("title").GetString());
        Assert.Equal("Old note", document.RootElement.GetProperty("trip").GetProperty("noteText").GetString());
    }

    [Fact]
    public async Task PatchTrip_Returns404_WhenTripDoesNotExist()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        var response = await client.PatchAsync(
            $"/trips/{Guid.NewGuid()}",
            new StringContent("""{ "title": "Any title" }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PatchTrip_ClearsNoteText_WhenNoteTextIsNull()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync(noteText: "Will be cleared");

        using var client = _factory.CreateClient();
        var response = await client.PatchAsync(
            $"/trips/{trip.Id}",
            new StringContent("""{ "noteText": null }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("trip").GetProperty("noteText").ValueKind);
    }

    [Fact]
    public async Task PatchTrip_DoesNotChangeNoteText_WhenNoteTextFieldIsMissing()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync(noteText: "Keep me");

        using var client = _factory.CreateClient();
        var response = await client.PatchAsync(
            $"/trips/{trip.Id}",
            new StringContent("""{ "placeText": "Rome" }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("Keep me", document.RootElement.GetProperty("trip").GetProperty("noteText").GetString());
    }

    [Fact]
    public async Task PatchTrip_RemovesAllTags_WhenTagsIsEmptyArray()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        var tagA = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "beach", "Beach");
        var tagB = CreateTag(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "city", "City");
        await SeedTagsAsync(tagA, tagB);

        var trip = await SeedTripAsync(hasAnyTags: true);
        await SeedTripTagsAsync(trip.Id, tagA.Id, tagB.Id);

        using var client = _factory.CreateClient();
        var response = await client.PatchAsync(
            $"/trips/{trip.Id}",
            new StringContent("""{ "tags": [] }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.TripTags.CountAsync(x => x.TripId == trip.Id);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task PatchTrip_Returns400_WhenTagDoesNotExist()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync();

        using var client = _factory.CreateClient();
        var response = await client.PatchAsync(
            $"/trips/{trip.Id}",
            new StringContent(
                """{ "tags": [ { "tagId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1", "order": 1 } ] }""",
                Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PatchTrip_Returns400_WhenMergedDatesAreInvalid()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync(dateFrom: new DateOnly(2026, 5, 10), dateTo: new DateOnly(2026, 5, 15));

        using var client = _factory.CreateClient();
        var response = await client.PatchAsync(
            $"/trips/{trip.Id}",
            new StringContent("""{ "dateTo": "2026-05-01" }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchTrip_Returns400_WhenTitleIsNull()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync();

        using var client = _factory.CreateClient();
        var response = await client.PatchAsync(
            $"/trips/{trip.Id}",
            new StringContent("""{ "title": null }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        var result = User.Create("patch-trips-tests@example.com", "test-password-hash");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var user = result.Value!;
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, TripsEndpoints.DevelopmentUserId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task<Trip> SeedTripAsync(
        string title = "Trip title",
        string? placeText = "Paris",
        string? noteText = "Trip note",
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        bool hasAnyTags = false)
    {
        var createResult = Trip.Create(
            TripsEndpoints.DevelopmentUserId,
            title,
            placeText,
            noteText,
            dateFrom ?? new DateOnly(2026, 5, 1),
            dateTo ?? new DateOnly(2026, 5, 7),
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

    private async Task SeedTripTagsAsync(Guid tripId, params Guid[] tagIds)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tripTags = new List<TripTag>();
        for (var i = 0; i < tagIds.Length; i++)
        {
            var tripTagResult = TripTag.Create(tripId, tagIds[i], i);
            Assert.True(tripTagResult.IsSuccess);
            Assert.NotNull(tripTagResult.Value);
            tripTags.Add(tripTagResult.Value!);
        }

        db.TripTags.AddRange(tripTags);
        await db.SaveChangesAsync();
    }

    private static Tag CreateTag(Guid id, string code, string displayName)
    {
        var tag = (Tag)Activator.CreateInstance(typeof(Tag), nonPublic: true)!;

        typeof(Tag).GetProperty(nameof(Tag.Id))!.SetValue(tag, id);
        typeof(Tag).GetProperty(nameof(Tag.Code))!.SetValue(tag, code);
        typeof(Tag).GetProperty(nameof(Tag.DisplayName))!.SetValue(tag, displayName);
        typeof(Tag).GetProperty(nameof(Tag.CreatedAt))!.SetValue(tag, DateTimeOffset.UtcNow);

        return tag;
    }
}
