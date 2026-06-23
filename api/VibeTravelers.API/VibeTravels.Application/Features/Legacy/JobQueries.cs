using System;
using VibeTravels.Application.Features.Legacy.Jobs;

namespace VibeTravels.Application.Features.Jobs.Queries;

public sealed record GetGenerationJobQuery(Guid JobId);

public sealed record ListTripJobsQuery(Guid TripId, ListTripJobsRequest Request);
