using System;

namespace VibeTravels.Application.Features.Legacy.Tags.Models;

public sealed record TagDto(Guid Id, string Code, string DisplayName, DateTimeOffset CreatedAt);
