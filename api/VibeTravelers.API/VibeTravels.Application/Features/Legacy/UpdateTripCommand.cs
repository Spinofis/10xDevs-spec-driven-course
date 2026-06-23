using System;
using System.Collections.Generic;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Legacy.Trips.Models;

namespace VibeTravels.Application.Features.Legacy.Trips.Commands;

public sealed record UpdateTripCommandRequest(
    Guid TripId,
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
    IReadOnlyList<TripTagInputDto>? Tags);

public sealed record UpdateTripCommandResponse(TripDto Trip);
