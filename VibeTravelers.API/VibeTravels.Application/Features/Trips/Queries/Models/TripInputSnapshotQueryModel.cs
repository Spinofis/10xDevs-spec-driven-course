using System.Text.Json;
using VibeTravels.Application.Features.Common;

namespace VibeTravels.Application.Features.Trips.Queries.Models;

public sealed record TripInputSnapshotQueryModel(
    Guid Id,
    InputSnapshotKind Kind,
    int GenerationNo,
    JsonElement Payload,
    DateTimeOffset CreatedAt);
