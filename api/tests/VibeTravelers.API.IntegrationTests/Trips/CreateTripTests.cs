using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class CreateTripTests
{
    private readonly ApiFactory _factory;

    public CreateTripTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task CreateTrip_Returns201AndPayload_WhenRequestIsValid()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        var tagA = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "beach", "Beach", DateTimeOffset.UtcNow);
        var tagB = CreateTag(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "museums", "Museums", DateTimeOffset.UtcNow);
        await SeedTagsAsync(tagA, tagB);

        using var client = _factory.CreateClient();
        const string correlationId = "corr-trip-create-1";

        var json = """
        {
          "model": {
            "title": "Trip to Rome",
            "placeText": "Rome, Italy",
            "noteText": "Food and museums",
            "dateFrom": "2026-05-01",
            "dateTo": "2026-05-07",
            "stayLengthMinDays": 5,
            "stayLengthMaxDays": 7,
            "peopleCount": 2,
            "budgetLevel": "medium",
            "pace": "normal",
            "tags": [
              { "tagId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1", "order": 2 },
              { "tagId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1", "order": 1 }
            ]
          }
        }
        """;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/trips")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        Assert.Equal("Trip to Rome", root.GetProperty("trip").GetProperty("title").GetString());
        Assert.Equal("Rome, Italy", root.GetProperty("trip").GetProperty("placeText").GetString());
        Assert.Equal("medium", root.GetProperty("trip").GetProperty("budgetLevel").GetString());
        Assert.Equal("normal", root.GetProperty("trip").GetProperty("pace").GetString());
        Assert.False(root.GetProperty("trip").GetProperty("hasGeneratedPlan").GetBoolean());

        var tags = root.GetProperty("tags");
        Assert.Equal(2, tags.GetArrayLength());
        Assert.Equal("beach", tags[0].GetProperty("tag").GetProperty("code").GetString());
        Assert.Equal(1, tags[0].GetProperty("order").GetInt32());
        Assert.Equal("museums", tags[1].GetProperty("tag").GetProperty("code").GetString());
        Assert.Equal(2, tags[1].GetProperty("order").GetInt32());
    }

    [Fact]
    public async Task CreateTrip_Returns400_WhenValidationFails()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();

        var json = """
        {
          "model": {
            "title": "",
            "placeText": "",
            "dateFrom": "2026-05-10",
            "dateTo": "2026-05-01",
            "stayLengthMinDays": 0,
            "stayLengthMaxDays": -1,
            "peopleCount": 0,
            "tags": []
          }
        }
        """;

        var response = await client.PostAsync("/trips", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateTrip_Returns201_WhenPlaceTextIsMissingButNoteTextIsProvided()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();

        var json = """
        {
          "model": {
            "title": "Trip without place text",
            "placeText": null,
            "noteText": "We will decide exact place later",
            "dateFrom": "2026-05-01",
            "dateTo": "2026-05-07",
            "stayLengthMinDays": 5,
            "stayLengthMaxDays": 7,
            "peopleCount": 2,
            "budgetLevel": "medium",
            "pace": "normal",
            "tags": []
          }
        }
        """;

        var response = await client.PostAsync("/trips", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("trip").GetProperty("placeText").ValueKind);
        Assert.Equal("We will decide exact place later", document.RootElement.GetProperty("trip").GetProperty("noteText").GetString());
    }

    [Fact]
    public async Task CreateTrip_Returns404_WhenTagDoesNotExist()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();

        var json = """
        {
          "model": {
            "title": "Trip to Rome",
            "placeText": "Rome, Italy",
            "dateFrom": "2026-05-01",
            "dateTo": "2026-05-07",
            "stayLengthMinDays": 5,
            "stayLengthMaxDays": 7,
            "peopleCount": 2,
            "tags": [
              { "tagId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1", "order": 0 }
            ]
          }
        }
        """;

        var response = await client.PostAsync("/trips", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TAG_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
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
        var result = User.Create("trips-tests@example.com", "test-password-hash");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var user = result.Value!;
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, TripsEndpoints.DevelopmentUserId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task SeedTagsAsync(params Tag[] tags)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Tags.AddRange(tags);
        await db.SaveChangesAsync();
    }

    private static Tag CreateTag(Guid id, string code, string displayName, DateTimeOffset createdAt)
    {
        var tag = (Tag)Activator.CreateInstance(typeof(Tag), nonPublic: true)!;

        typeof(Tag).GetProperty(nameof(Tag.Id))!.SetValue(tag, id);
        typeof(Tag).GetProperty(nameof(Tag.Code))!.SetValue(tag, code);
        typeof(Tag).GetProperty(nameof(Tag.DisplayName))!.SetValue(tag, displayName);
        typeof(Tag).GetProperty(nameof(Tag.CreatedAt))!.SetValue(tag, createdAt);

        return tag;
    }
}
