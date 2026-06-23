using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Legacy.Me.Models;

namespace VibeTravels.Application.Features.Legacy.Me.Commands;

public sealed record UpsertUserProfileCommandRequest(
    BudgetLevel? DefaultBudgetLevel,
    int? DefaultPeopleCount,
    Pace? DefaultPace,
    string? DefaultNotes,
    bool IsDefault);

public sealed record UpsertUserProfileCommandResponse(UserProfileDto Profile);
