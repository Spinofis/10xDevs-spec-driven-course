using System;
using VibeTravels.Application.Features.Legacy.Tags.Models;

namespace VibeTravels.Application.Features.Legacy.Trips.Models;

public sealed record TripTagDto(TagDto Tag, int? Order, DateTimeOffset CreatedAt);
