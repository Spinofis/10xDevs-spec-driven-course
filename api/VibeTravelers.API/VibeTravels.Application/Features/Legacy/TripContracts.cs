using System;
using System.Collections.Generic;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Legacy.Tags;

namespace VibeTravels.Application.Features.Legacy.Trips;

public sealed record TripDto(
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

public sealed record TripTagDto(
    TagDto Tag,
    int? Order,
    DateTimeOffset CreatedAt);

public sealed record TripTagRequest(Guid TagId, int? Order);

public record TripDetailsResponse(
    TripDto Trip,
    IReadOnlyList<TripTagDto> Tags);

public sealed record CreateTripRequest(
    string Title,
    string PlaceText,
    string? NoteText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int? StayLengthMinDays,
    int? StayLengthMaxDays,
    int? PeopleCount,
    BudgetLevel? BudgetLevel,
    Pace? Pace,
    IReadOnlyList<TripTagRequest>? Tags);

public sealed record CreateTripResponse(
    TripDto Trip,
    IReadOnlyList<TripTagDto> Tags)
    : TripDetailsResponse(Trip, Tags);

public sealed record ListTripsRequest(
    string? Q,
    bool? HasPlan,
    bool? IncludeDeleted,
    int? Limit,
    string? Cursor,
    string? Sort)
    : IPagedRequest, ISortableRequest;

public sealed record ListTripsResponse(IReadOnlyList<TripDto> Items)
    : PagedResponse<TripDto>(Items);

public sealed record GetTripResponse(
    TripDto Trip,
    IReadOnlyList<TripTagDto> Tags)
    : TripDetailsResponse(Trip, Tags);

public sealed record UpdateTripRequest(
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
    IReadOnlyList<TripTagRequest>? Tags);

public sealed record UpdateTripResponse(TripDto Trip);

public sealed record DeleteTripResponse;
