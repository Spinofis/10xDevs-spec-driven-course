using System;
using System.Collections.Generic;
using System.Text.Json;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Legacy.Trips.Models;

public sealed record TripInputSnapshotDto(
    Guid Id,
    InputSnapshotKind Kind,
    int GenerationNo,
    JsonElement Payload,
    DateTimeOffset CreatedAt);
