using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Jobs.Queries.Models;

public sealed record QueuedGenerationJobQueryModel(
    Guid Id,
    Guid TripId,
    GenerationJobStatus Status,
    DateTimeOffset RequestedAt,
    int AttemptNo);
