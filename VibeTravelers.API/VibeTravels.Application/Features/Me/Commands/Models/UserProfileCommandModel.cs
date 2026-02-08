using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Me.Commands.Models;

public sealed record UserProfileCommandModel(
    BudgetLevel? DefaultBudgetLevel,
    int? DefaultPeopleCount,
    Pace? DefaultPace,
    string? DefaultNotes,
    bool IsDefault);
