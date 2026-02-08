using System;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Jobs.Models;

public sealed record GenerationJobDto(
    Guid Id,
    Guid TripId,
    GenerationJobStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? ErrorMessage,
    bool Discarded,
    string? DiscardReason);
