using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Me;

[Collection("Database")]
public sealed class GetUserProfileTests
{
    private readonly ApiFactory _factory;

    public GetUserProfileTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task GetProfile_Returns200WithDefaults_WhenNoProfileExists()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/me/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        Assert.Equal(TripsEndpoints.DevelopmentUserId.ToString(), root.GetProperty("userId").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("profile").GetProperty("defaultBudgetLevel").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("profile").GetProperty("defaultPeopleCount").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("profile").GetProperty("defaultPace").ValueKind);
        Assert.True(root.GetProperty("profile").GetProperty("isDefault").GetBoolean());
        Assert.Equal(0, root.GetProperty("preferenceTags").GetArrayLength());
    }

    [Fact]
    public async Task GetProfile_Returns200WithStoredData_WhenProfileExists()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        var tag = CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "beach", "Beach");
        await SeedTagAsync(tag);
        await SeedProfileAsync(BudgetLevel: "Medium", PeopleCount: 2, Pace: "Normal", Notes: "My notes", tagId: tag.Id);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/me/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        Assert.Equal("medium", root.GetProperty("profile").GetProperty("defaultBudgetLevel").GetString());
        Assert.Equal(2, root.GetProperty("profile").GetProperty("defaultPeopleCount").GetInt32());
        Assert.Equal("normal", root.GetProperty("profile").GetProperty("defaultPace").GetString());
        Assert.Equal("My notes", root.GetProperty("profile").GetProperty("defaultNotes").GetString());
        Assert.Equal(1, root.GetProperty("preferenceTags").GetArrayLength());
        Assert.Equal("beach", root.GetProperty("preferenceTags")[0].GetProperty("tag").GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetProfile_ReturnsCorrelationId()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        const string correlationId = "corr-me-get-1";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/me/profile");
        request.Headers.Add("X-Correlation-Id", correlationId);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
        var result = User.Create("me-tests@example.com", "test-password-hash");
        Assert.True(result.IsSuccess);
        var user = result.Value!;
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, TripsEndpoints.DevelopmentUserId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task SeedTagAsync(Tag tag)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
    }

    private async Task SeedProfileAsync(string? BudgetLevel, int? PeopleCount, string? Pace, string? Notes, Guid? tagId = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = UserProfile.Create(TripsEndpoints.DevelopmentUserId);
        profile.Update(BudgetLevel, PeopleCount, Pace, Notes, true);
        db.UserProfiles.Add(profile);

        if (tagId.HasValue)
            db.UserPreferenceTags.Add(UserPreferenceTag.Create(TripsEndpoints.DevelopmentUserId, tagId.Value, 1));

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
