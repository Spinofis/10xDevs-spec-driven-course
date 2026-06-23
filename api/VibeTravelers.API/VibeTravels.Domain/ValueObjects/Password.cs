using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Domain.ValueObjects;

public sealed record Password
{
    public const int MaxLength = 1024;

    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    public static Result<Password> Create(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Result<Password>.Fail(ResultErrors.Validation("Password is required.", nameof(Password)));
        }

        if (input.Length > MaxLength)
        {
            return Result<Password>.Fail(ResultErrors.Validation($"Password must be at most {MaxLength} characters.", nameof(Password)));
        }

        return Result<Password>.Ok(new Password(input));
    }

    public static Password From(string input)
    {
        var result = Create(input);
        if (result.IsSuccess && result.Value is not null)
            return result.Value;

        throw new InvalidOperationException(result.Errors.Count > 0 ? result.Errors[0].Message : "Invalid password.");
    }

    public override string ToString() => "[REDACTED]";
}
