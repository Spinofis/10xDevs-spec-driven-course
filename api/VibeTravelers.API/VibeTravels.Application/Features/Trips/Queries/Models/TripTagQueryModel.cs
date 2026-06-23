using VibeTravels.Application.Features.Tags.Queries.Models;

namespace VibeTravels.Application.Features.Trips.Queries.Models;

public sealed record TripTagQueryModel(TagQueryModel Tag, int? Order, DateTimeOffset CreatedAt);
