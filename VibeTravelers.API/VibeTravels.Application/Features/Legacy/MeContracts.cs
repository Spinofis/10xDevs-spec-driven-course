using System;
using System.Collections.Generic;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Legacy.Tags;

namespace VibeTravels.Application.Features.Legacy.Me;

public sealed record UserProfileDto(
    BudgetLevel? DefaultBudgetLevel,
    int? DefaultPeopleCount,
    Pace? DefaultPace,
    string? DefaultNotes,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UserProfileUpsertDto(
    BudgetLevel? DefaultBudgetLevel,
    int? DefaultPeopleCount,
    Pace? DefaultPace,
    string? DefaultNotes,
    bool IsDefault);

public sealed record PreferenceTagDto(
    TagDto Tag,
    int? Order,
    DateTimeOffset CreatedAt);

public sealed record PreferenceTagUpsertDto(
    Guid TagId,
    int Weight,
    int SortOrder);

public sealed record MeProfileResponse(
    Guid UserId,
    UserProfileDto Profile,
    IReadOnlyList<PreferenceTagDto> PreferenceTags);

public sealed record UpsertMeProfileRequest(
    UserProfileUpsertDto Profile,
    IReadOnlyList<PreferenceTagUpsertDto> PreferenceTags);
