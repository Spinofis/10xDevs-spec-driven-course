using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Trips.Commands.Models;

public sealed record PatchTripCommandModel(
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
    OptionalField<IReadOnlyList<TripTagCommandModel>> Tags);
