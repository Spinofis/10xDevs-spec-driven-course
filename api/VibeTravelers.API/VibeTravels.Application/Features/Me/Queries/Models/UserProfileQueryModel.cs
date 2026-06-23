using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Me.Queries.Models;

public sealed record UserProfileQueryModel(
    BudgetLevel? DefaultBudgetLevel,
    int? DefaultPeopleCount,
    Pace? DefaultPace,
    string? DefaultNotes,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
