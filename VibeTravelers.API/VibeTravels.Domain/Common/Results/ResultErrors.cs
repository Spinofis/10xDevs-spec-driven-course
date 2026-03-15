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

    public static Error TagNotFound(string? target = null)
        => new(
            Code: "TAG_NOT_FOUND",
            Message: "One or more tags were not found.",
            Status: HttpStatusCode.NotFound,
            Target: target);

    public static Error TripNotFound(string? target = null)
        => new(
            Code: "TRIP_NOT_FOUND",
            Message: "Trip was not found.",
            Status: HttpStatusCode.NotFound,
            Target: target);

    public static Error GenerationRequirementsNotMet(string message, string? target = null)
        => new(
            Code: "GENERATION_REQUIREMENTS_NOT_MET",
            Message: message,
            Status: HttpStatusCode.BadRequest,
            Target: target);

    public static Error JobAlreadyActive(string? target = null)
        => new(
            Code: "JOB_ALREADY_ACTIVE",
            Message: "A generation job is already active for this trip.",
            Status: HttpStatusCode.Conflict,
            Target: target);

    public static Error Unknown(string? message = null, string? target = null)
        => new(
            Code: "UNKNOWN_ERROR",
            Message: message ?? "Unknown error.",
            Status: HttpStatusCode.InternalServerError,
            Target: target);
}
