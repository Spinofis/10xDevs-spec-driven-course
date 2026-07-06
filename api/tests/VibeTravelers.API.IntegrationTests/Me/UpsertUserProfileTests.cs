using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Me;

[Collection("Database")]
public sealed class UpsertUserProfileTests
{
    private readonly ApiFactory _factory;

    public UpsertUserProfileTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task UpsertProfile_Returns204_WhenRequestIsValid()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        var json = """
        {
          "profile": {
            "defaultBudgetLevel": "medium",
            "defaultPeopleCount": 2,
            "defaultPace": "normal",
            "defaultNotes": "Some notes",
            "isDefault": true
          },
          "preferenceTags": []
        }
        """;

        var response = await client.PutAsync("/me/profile",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpsertProfile_Returns204AndPersistsProfile_WhenCalledTwice()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        var json1 = """
        {
          "profile": { "defaultPeopleCount": 2, "isDefault": true },
          "preferenceTags": []
        }
        """;
        var json2 = """
        {
          "profile": { "defaultPeopleCount": 5, "isDefault": false },
          "preferenceTags": []
        }
        """;

        await client.PutAsync("/me/profile", new StringContent(json1, Encoding.UTF8, "application/json"));
        var response = await client.PutAsync("/me/profile", new StringContent(json2, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync("/me/profile");
        await using var stream = await getResponse.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal(5, doc.RootElement.GetProperty("profile").GetProperty("defaultPeopleCount").GetInt32());
        Assert.False(doc.RootElement.GetProperty("profile").GetProperty("isDefault").GetBoolean());
    }

    [Fact]
    public async Task UpsertProfile_Returns400_WhenPeopleCountIsZero()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        var json = """
        {
          "profile": { "defaultPeopleCount": 0, "isDefault": true },
          "preferenceTags": []
        }
        """;

        var response = await client.PutAsync("/me/profile",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("VALIDATION_ERROR", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpsertProfile_Returns404_WhenTagDoesNotExist()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        var json = """
        {
          "profile": { "isDefault": true },
          "preferenceTags": [
            { "tagId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1", "order": 1 }
          ]
        }
        """;

        var response = await client.PutAsync("/me/profile",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TAG_NOT_FOUND", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpsertProfile_ReplacesPreferenceTags_OnSecondCall()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        var tagA = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "beach", "Beach");
        var tagB = CreateTag(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "mountains", "Mountains");
        await SeedTagsAsync(tagA, tagB);

        using var client = _factory.CreateClient();

        var firstJson = $$"""
        {
          "profile": { "isDefault": true },
          "preferenceTags": [
            { "tagId": "{{tagA.Id}}", "order": 1 },
            { "tagId": "{{tagB.Id}}", "order": 2 }
          ]
        }
        """;
        await client.PutAsync("/me/profile", new StringContent(firstJson, Encoding.UTF8, "application/json"));

        var secondJson = $$"""
        {
          "profile": { "isDefault": true },
          "preferenceTags": [
            { "tagId": "{{tagB.Id}}", "order": 1 }
          ]
        }
        """;
        await client.PutAsync("/me/profile", new StringContent(secondJson, Encoding.UTF8, "application/json"));

        var getResponse = await client.GetAsync("/me/profile");
        await using var stream = await getResponse.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        var tags = doc.RootElement.GetProperty("preferenceTags");
        Assert.Equal(1, tags.GetArrayLength());
        Assert.Equal("mountains", tags[0].GetProperty("tag").GetProperty("code").GetString());
        Assert.Equal(1, tags[0].GetProperty("order").GetInt32());
    }

    [Fact]
    public async Task UpsertProfile_ReturnsCorrelationId()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        const string correlationId = "corr-me-upsert-1";

        using var request = new HttpRequestMessage(HttpMethod.Put, "/me/profile")
        {
            Content = new StringContent(
                """{ "profile": { "isDefault": true }, "preferenceTags": [] }""",
                Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Correlation-Id", correlationId);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());
    }

    private async Task ResetDatabaseAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.UserPreferenceTags.RemoveRange(db.UserPreferenceTags);
        db.UserProfiles.RemoveRange(db.UserProfiles);
        db.Tags.RemoveRange(db.Tags);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }

    private async Task SeedDevelopmentUserAsync()
    {
        var result = User.Create("me-upsert-tests@example.com", "test-password-hash");
        Assert.True(result.IsSuccess);
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
