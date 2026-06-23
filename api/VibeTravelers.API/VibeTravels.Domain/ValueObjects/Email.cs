using System.Net.Mail;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Domain.ValueObjects;

public sealed record Email
{
    public const int MaxLength = 256;

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result<Email>.Fail(ResultErrors.Validation("Email is required.", nameof(Email)));
        }

        var normalized = input.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return Result<Email>.Fail(ResultErrors.Validation($"Email must be at most {MaxLength} characters.", nameof(Email)));
        }

        if (MailAddress.TryCreate(normalized, out _) is false)
        {
            return Result<Email>.Fail(ResultErrors.Validation("Invalid email format.", nameof(Email)));
        }

        return Result<Email>.Ok(new Email(normalized));
    }

    public static Email From(string input)
    {
        var result = Create(input);
        if (result.IsSuccess && result.Value is not null)
            return result.Value;

        throw new InvalidOperationException(result.Errors.Count > 0 ? result.Errors[0].Message : "Invalid email.");
    }
}
