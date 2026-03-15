using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VibeTravelers.API.Endpoints;
using VibeTravels.Domain.Entities.Trips;
using VibeTravels.Domain.Entities.Users;
using VibeTravels.Infrastructure.Persistence;

namespace VibeTravelers.API.IntegrationTests.Trips;

[Collection("Database")]
public sealed class DeleteTripTests
{
    private readonly ApiFactory _factory;

    public DeleteTripTests(DatabaseFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task DeleteTrip_Returns204_AndSoftDeletesTrip()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync();

        using var client = _factory.CreateClient();
        const string correlationId = "corr-trip-delete-1";
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/trips/{trip.Id}");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Trips.SingleAsync(x => x.Id == trip.Id);
        Assert.NotNull(saved.DeletedAt);
    }

    [Fact]
    public async Task DeleteTrip_Returns404_WhenTripDoesNotExist()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/trips/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("TRIP_NOT_FOUND", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task DeleteTrip_Returns404_WhenTripIsAlreadyDeleted()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Trips.SingleAsync(x => x.Id == trip.Id);
            var result = entity.SoftDelete(DateTimeOffset.UtcNow);
            Assert.True(result.IsSuccess);
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/trips/{trip.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTrip_Returns400_WhenTripIdIsEmptyGuid()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();

        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/trips/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task DeleteTrip_RemovesTripFromDefaultList_ButKeepsWithIncludeDeleted()
    {
        await ResetDatabaseAsync();
        await SeedDevelopmentUserAsync();
        var trip = await SeedTripAsync();

        using var client = _factory.CreateClient();
        var deleteResponse = await client.DeleteAsync($"/trips/{trip.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listWithoutDeleted = await client.GetAsync("/trips");
        Assert.Equal(HttpStatusCode.OK, listWithoutDeleted.StatusCode);

        await using (var stream = await listWithoutDeleted.Content.ReadAsStreamAsync())
        using (var document = await JsonDocument.ParseAsync(stream))
        {
            Assert.Equal(0, document.RootElement.GetProperty("items").GetArrayLength());
        }

        var listWithDeleted = await client.GetAsync("/trips?includeDeleted=true");
        Assert.Equal(HttpStatusCode.OK, listWithDeleted.StatusCode);

        await using var withDeletedStream = await listWithDeleted.Content.ReadAsStreamAsync();
        using var withDeletedDoc = await JsonDocument.ParseAsync(withDeletedStream);
        Assert.Equal(1, withDeletedDoc.RootElement.GetProperty("items").GetArrayLength());
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
        var result = User.Create("delete-trips-tests@example.com", "test-password-hash");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var user = result.Value!;
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, TripsEndpoints.DevelopmentUserId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private async Task<Trip> SeedTripAsync()
    {
        var createResult = Trip.Create(
            TripsEndpoints.DevelopmentUserId,
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
}
