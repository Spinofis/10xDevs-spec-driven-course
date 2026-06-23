using System.Collections.Generic;
using VibeTravels.Application.Features.Common;
using VibeTravels.Application.Features.Legacy.Trips.Models;

namespace VibeTravels.Application.Features.Legacy.Trips.Queries;

public sealed record ListTripsQueryResponse(IReadOnlyList<TripDto> Items)
    : PagedResponse<TripDto>(Items);
