using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Application.Tests.Trips;

public sealed class TripSoftDeleteTests
{
    [Fact]
    public void SoftDelete_SetsDeletedAtAndUpdatedAt()
    {
        var trip = CreateTrip();
        var now = new DateTimeOffset(2026, 03, 01, 12, 00, 00, TimeSpan.Zero);

        var result = trip.SoftDelete(now);

        Assert.True(result.IsSuccess);
        Assert.Equal(now, trip.DeletedAt);
        Assert.Equal(now, trip.UpdatedAt);
    }

    [Fact]
    public void SoftDelete_Fails_WhenAlreadyDeleted()
    {
        var trip = CreateTrip();
        var firstDeleteAt = new DateTimeOffset(2026, 03, 01, 12, 00, 00, TimeSpan.Zero);
        var secondDeleteAt = new DateTimeOffset(2026, 03, 02, 12, 00, 00, TimeSpan.Zero);

        var first = trip.SoftDelete(firstDeleteAt);
        var second = trip.SoftDelete(secondDeleteAt);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal(firstDeleteAt, trip.DeletedAt);
    }

    private static Trip CreateTrip()
    {
        var createResult = Trip.Create(
            userId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            title: "Trip title",
            placeText: "Paris",
            noteText: "Note",
            dateFrom: new DateOnly(2026, 5, 10),
            dateTo: new DateOnly(2026, 5, 15),
            stayLengthMinDays: 3,
            stayLengthMaxDays: 7,
            peopleCount: 2,
            budgetLevel: "Medium",
            pace: "Normal",
            hasAnyTags: false);

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(createResult.Value);
        return createResult.Value!;
    }
}
