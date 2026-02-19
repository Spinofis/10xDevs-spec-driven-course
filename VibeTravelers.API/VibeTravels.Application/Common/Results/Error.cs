using System.Net;

namespace VibeTravels.Application.Common.Results;

public readonly record struct Error(
    string Code,
    string Message,
    HttpStatusCode Status,
    string? Target = null
);

