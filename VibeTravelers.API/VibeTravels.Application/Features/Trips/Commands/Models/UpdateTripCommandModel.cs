using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Trips.Commands.Models;

public sealed record UpdateTripCommandModel(
    string? Title,
    string? PlaceText,
    string? NoteText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int? StayLengthMinDays,
    int? StayLengthMaxDays,
    int? PeopleCount,
    BudgetLevel? BudgetLevel,
    Pace? Pace,
    IReadOnlyList<TripTagCommandModel>? Tags);
