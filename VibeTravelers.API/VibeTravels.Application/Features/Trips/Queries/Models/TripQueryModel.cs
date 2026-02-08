using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Trips.Queries.Models;

public sealed record TripQueryModel(
    Guid Id,
    Guid UserId,
    string Title,
    string? PlaceText,
    string? NoteText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int? StayLengthMinDays,
    int? StayLengthMaxDays,
    int? PeopleCount,
    BudgetLevel? BudgetLevel,
    Pace? Pace,
    DateTimeOffset? GeneratedAt,
    bool HasGeneratedPlan,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
