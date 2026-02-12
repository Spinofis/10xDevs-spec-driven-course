using System;
using System.Collections.Generic;
using System.Text.Json;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Trips;

// Payload preserves the raw JSON snapshot (jsonb in generation_jobs.input_snapshot or a future snapshots table).
public sealed record TripInputSnapshotDto(
    Guid Id,
    InputSnapshotKind Kind,
    int GenerationNo,
    JsonElement Payload,
    DateTimeOffset CreatedAt);

public sealed record ListTripInputSnapshotsRequest(int? Limit, string? Cursor, string? Sort)
    : IPagedRequest, ISortableRequest;

public sealed record ListTripInputSnapshotsResponse(IReadOnlyList<TripInputSnapshotDto> Items)
    : PagedResponse<TripInputSnapshotDto>(Items);
