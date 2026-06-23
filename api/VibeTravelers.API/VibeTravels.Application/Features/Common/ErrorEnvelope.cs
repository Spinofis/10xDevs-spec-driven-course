namespace VibeTravels.Application.Features.Common;

public sealed record ErrorEnvelope(ErrorDetails Error);

public sealed record ErrorDetails(
    string Code,
    string Message,
    IReadOnlyDictionary<string, object?>? Details,
    string? TraceId);
