using System;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Jobs.Models;

public sealed record GenerationJobListItemDto(
    Guid Id,
    GenerationJobStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? FinishedAt,
    bool Discarded,
    string? DiscardReason);
