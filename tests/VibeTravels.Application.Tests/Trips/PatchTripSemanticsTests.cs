using System.Text.Json;
using VibeTravels.Application.Features.Trips.Commands;
using VibeTravels.Domain.Entities.Trips;

namespace VibeTravels.Application.Tests.Trips;

public sealed class PatchTripSemanticsTests
{
    [Fact]
    public void OptionalField_DistinguishesMissingFromNull()
    {
        var missing = Deserialize("""{}""");
        var nullValue = Deserialize("""{ "noteText": null }""");

        Assert.False(missing.NoteText.IsSet);
        Assert.Throws<InvalidOperationException>(() => _ = missing.NoteText.Value);

        Assert.True(nullValue.NoteText.IsSet);
        Assert.Null(nullValue.NoteText.Value);
    }

    [Fact]
    public void ApplyPatch_Fails_WhenMergedDatesAreInvalid()
    {
        var trip = CreateTrip();

        var result = trip.ApplyPatch(
            title: trip.Title.Value,
            placeText: trip.PlaceText?.Value,
            noteText: trip.NoteText,
            dateFrom: trip.DateFrom,
            dateTo: new DateOnly(2026, 5, 1),
            stayLengthMinDays: trip.StayLengthMinDays,
            stayLengthMaxDays: trip.StayLengthMaxDays,
            peopleCount: trip.PeopleCount,
            budgetLevel: trip.BudgetLevel,
            pace: trip.Pace,
            hasAnyTags: false);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ApplyPatch_Fails_WhenMergedStateHasNoPlaceNoteAndTags()
    {
        var trip = CreateTrip();

        var result = trip.ApplyPatch(
            title: trip.Title.Value,
            placeText: null,
            noteText: null,
            dateFrom: trip.DateFrom,
            dateTo: trip.DateTo,
            stayLengthMinDays: trip.StayLengthMinDays,
            stayLengthMaxDays: trip.StayLengthMaxDays,
            peopleCount: trip.PeopleCount,
            budgetLevel: trip.BudgetLevel,
            pace: trip.Pace,
            hasAnyTags: false);

        Assert.False(result.IsSuccess);
    }

    private static PatchTripCommandRequest Deserialize(string json)
    {
        return JsonSerializer.Deserialize<PatchTripCommandRequest>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
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
