using System.Net;

namespace VibeTravels.Domain.Common.Results;

public static class ResultErrors
{
    public static Error EmailTaken(string? target = null)
        => new(
            Code: "EMAIL_TAKEN",
            Message: "Email is already registered.",
            Status: HttpStatusCode.Conflict,
            Target: target);

    public static Error Validation(string message, string? target = null)
        => new(
            Code: "VALIDATION_ERROR",
            Message: message,
            Status: HttpStatusCode.BadRequest,
            Target: target);

    public static Error Unknown(string? message = null, string? target = null)
        => new(
            Code: "UNKNOWN_ERROR",
            Message: message ?? "Unknown error.",
            Status: HttpStatusCode.InternalServerError,
            Target: target);
}

