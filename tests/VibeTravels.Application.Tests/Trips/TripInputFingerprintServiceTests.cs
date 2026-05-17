using VibeTravels.Application.Features.Trips.Services;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Application.Tests.Trips;

public sealed class TripInputFingerprintServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Build_ReturnsDeterministicPayloadAndHash_ForSameTripInput()
    {
        var trip = CreateTrip();
        var service = new TripInputFingerprintService();

        var first = service.Build(trip, UserId);
        var second = service.Build(trip, UserId);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotNull(first.Value);
        Assert.NotNull(second.Value);
        Assert.Equal(first.Value!.PayloadJson, second.Value!.PayloadJson);
        Assert.Equal(first.Value.Hash, second.Value.Hash);
    }

    [Fact]
    public void Build_ReturnsDifferentHash_WhenTripInputChanges()
    {
        var trip = CreateTrip();
        var service = new TripInputFingerprintService();

        var before = service.Build(trip, UserId);
        var patchResult = trip.ApplyPatch(
            title: "Updated plan trip",
            placeText: "Porto",
            noteText: "Food and architecture",
            dateFrom: new DateOnly(2026, 8, 1),
            dateTo: new DateOnly(2026, 8, 4),
            stayLengthMinDays: 2,
            stayLengthMaxDays: 4,
            peopleCount: 2,
            budgetLevel: "Medium",
            pace: "Normal",
            hasAnyTags: false);
        var after = service.Build(trip, UserId);

        Assert.True(before.IsSuccess);
        Assert.True(patchResult.IsSuccess);
        Assert.True(after.IsSuccess);
        Assert.NotNull(before.Value);
        Assert.NotNull(after.Value);
        Assert.NotEqual(before.Value!.Hash, after.Value!.Hash);
    }

    private static Trip CreateTrip()
    {
        var result = Trip.Create(
            UserId,
            title: "Plan trip",
            placeText: "Porto",
            noteText: "Food and architecture",
            dateFrom: new DateOnly(2026, 8, 1),
            dateTo: new DateOnly(2026, 8, 4),
            stayLengthMinDays: 2,
            stayLengthMaxDays: 4,
            peopleCount: 2,
            budgetLevel: "Medium",
            pace: "Normal",
            hasAnyTags: false);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        return result.Value!;
    }
}
