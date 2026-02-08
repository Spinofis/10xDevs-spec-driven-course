using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Me.Models;

public sealed record UserProfileDto(
    BudgetLevel? DefaultBudgetLevel,
    int? DefaultPeopleCount,
    Pace? DefaultPace,
    string? DefaultNotes,
    bool IsDefault);
