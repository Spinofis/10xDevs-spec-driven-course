using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Jobs.Queries.Models;

public sealed record GenerationJobQueryModel(
    Guid Id,
    Guid TripId,
    GenerationJobStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int AttemptNo,
    string? ErrorCode,
    string? ErrorMessage,
    bool Discarded,
    string? DiscardReason);
