using System;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Jobs;

public sealed record QueueGenerationJobRequest;

public sealed record JobQueuedDto(
    Guid Id,
    Guid TripId,
    GenerationJobStatus Status,
    DateTimeOffset RequestedAt,
    int AttemptNo);

public sealed record QueueGenerationJobResponse(JobQueuedDto Job);

public record GenerationJobDto(
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

public sealed record GetGenerationJobResponse(
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
    string? DiscardReason)
    : GenerationJobDto(
        Id,
        TripId,
        Status,
        RequestedAt,
        StartedAt,
        FinishedAt,
        AttemptNo,
        ErrorCode,
        ErrorMessage,
        Discarded,
        DiscardReason);

public sealed record TripJobListItemDto(
    Guid Id,
    GenerationJobStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? FinishedAt,
    bool Discarded,
    string? DiscardReason);

public sealed record ListTripJobsRequest(int? Limit, string? Cursor, string? Sort)
    : IPagedRequest, ISortableRequest;

public sealed record ListTripJobsResponse(IReadOnlyList<TripJobListItemDto> Items)
    : PagedResponse<TripJobListItemDto>(Items);
