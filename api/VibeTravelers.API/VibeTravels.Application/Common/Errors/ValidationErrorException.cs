using System.Collections.Generic;
using System.Net;

namespace VibeTravels.Application.Common.Errors;

public sealed class ValidationErrorException : AppException
{
    public const string ErrorCode = "VALIDATION_ERROR";
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationErrorException(IReadOnlyDictionary<string, string[]> errors)
        : base("Validation failed.", ErrorCode, HttpStatusCode.BadRequest)
    {
        Errors = errors;
    }
}
