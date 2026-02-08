namespace VibeTravels.Application.Common.Errors;

public sealed class ValidationErrorException : Exception
{
    public const string ErrorCode = "VALIDATION_ERROR";
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationErrorException(IReadOnlyDictionary<string, string[]> errors)
        : base("Validation failed.")
    {
        Errors = errors;
    }
}
