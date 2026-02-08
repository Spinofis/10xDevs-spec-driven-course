using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Jobs.Queries.Models;

public sealed record GenerationJobListItemQueryModel(
    Guid Id,
    GenerationJobStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? FinishedAt,
    bool Discarded,
    string? DiscardReason);
