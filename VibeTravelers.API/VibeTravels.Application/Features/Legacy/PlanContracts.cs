using System;
using System.Collections.Generic;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Plans;

public sealed record PlanItemDto(
    Guid Id,
    int DayNumber,
    int Order,
    string Title,
    string? Description,
    string? LocationText,
    TimeOnly? EndTime,
    int? DurationMinutes,
    CostLevel? CostLevel,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record PlanDto(
    Guid TripId,
    int Version,
    PlanStatus Status,
    Guid? GeneratedFromJobId,
    DateTimeOffset? GeneratedAt,
    DateTimeOffset? SavedAt,
    string? Summary,
    IReadOnlyList<PlanItemDto> Items);

public sealed record GetTripPlanResponse(
    Guid TripId,
    int Version,
    PlanStatus Status,
    Guid? GeneratedFromJobId,
    DateTimeOffset? GeneratedAt,
    DateTimeOffset? SavedAt,
    string? Summary,
    IReadOnlyList<PlanItemDto> Items)
    : PlanDto(
        TripId,
        Version,
        Status,
        GeneratedFromJobId,
        GeneratedAt,
        SavedAt,
        Summary,
        Items);

public sealed record PlanItemInputDto(
    int DayNumber,
    int Order,
    string Title,
    string? Description,
    string? LocationText,
    TimeOnly? EndTime,
    int? DurationMinutes,
    CostLevel? CostLevel,
    IReadOnlyList<string> Tags);

public sealed record ReplacePlanRequest(
    string? Summary,
    IReadOnlyList<PlanItemInputDto> Items);

public sealed record ReplacePlanResponse(
    Guid TripId,
    int Version,
    PlanStatus Status,
    Guid? GeneratedFromJobId,
    DateTimeOffset? GeneratedAt,
    DateTimeOffset? SavedAt,
    string? Summary,
    IReadOnlyList<PlanItemDto> Items)
    : PlanDto(
        TripId,
        Version,
        Status,
        GeneratedFromJobId,
        GeneratedAt,
        SavedAt,
        Summary,
        Items);
