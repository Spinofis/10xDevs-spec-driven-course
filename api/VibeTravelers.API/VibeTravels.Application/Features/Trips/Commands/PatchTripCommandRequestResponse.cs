using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Trips.Commands.Models;
using VibeTravels.Application.Features.Trips.Queries.Models;

namespace VibeTravels.Application.Features.Trips.Commands;

public sealed record PatchTripCommandRequest(
    OptionalField<string> Title,
    OptionalField<string> PlaceText,
    OptionalField<string?> NoteText,
    OptionalField<DateOnly?> DateFrom,
    OptionalField<DateOnly?> DateTo,
    OptionalField<int> StayLengthMinDays,
    OptionalField<int> StayLengthMaxDays,
    OptionalField<int> PeopleCount,
    OptionalField<BudgetLevel?> BudgetLevel,
    OptionalField<Pace?> Pace,
    OptionalField<IReadOnlyList<TripTagCommandModel>> Tags)
{
    public PatchTripCommandModel ToModel()
        => new(
            Title,
            PlaceText,
            NoteText,
            DateFrom,
            DateTo,
            StayLengthMinDays,
            StayLengthMaxDays,
            PeopleCount,
            BudgetLevel,
            Pace,
            Tags);
}

public sealed record PatchTripCommandResponse(TripQueryModel Trip);
