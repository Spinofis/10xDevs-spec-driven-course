using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VibeTravels.Application.Features.Tags.Queries;
using VibeTravels.Domain.Entities.Tags;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Tags;

[Collection("Database")]
public sealed class GetTagsTests
{
    private readonly ApiFactory _factory;

    public GetTagsTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task GetTags_ReturnsEmptyList_WhenNoTagsExist()
    {
        await ResetTagsAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ListTagsQueryResponse>();

        Assert.NotNull(payload);
        Assert.Empty(payload!.Items);
    }

    [Fact]
    public async Task GetTags_ReturnsAllTags()
    {
        await ResetTagsAsync();
        await SeedTagsAsync(
            CreateTag(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "mountains", "Mountains", DateTimeOffset.UtcNow),
            CreateTag(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "beach", "Beach", DateTimeOffset.UtcNow));

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<ListTagsQueryResponse>();

        Assert.NotNull(items);
        Assert.Equal(2, items!.Items.Count);
        Assert.Equal("beach", items.Items[0].Code);
        Assert.Equal("mountains", items.Items[1].Code);
    }

    private async Task SeedTagsAsync(params Tag[] tags)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Tags.AddRange(tags);
        await db.SaveChangesAsync();
    }

    private async Task ResetTagsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Tags.RemoveRange(db.Tags);
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
