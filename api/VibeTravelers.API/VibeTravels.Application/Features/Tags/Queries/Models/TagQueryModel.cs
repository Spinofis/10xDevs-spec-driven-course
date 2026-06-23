namespace VibeTravels.Application.Features.Tags.Queries.Models;

public sealed record TagQueryModel(Guid Id, string Code, string DisplayName, DateTimeOffset CreatedAt);
