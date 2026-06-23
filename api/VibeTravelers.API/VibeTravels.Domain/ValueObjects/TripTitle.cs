using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Domain.ValueObjects;

public sealed record TripTitle
{
    public const int MaxLength = 200;

    public string Value { get; }

    private TripTitle(string value)
    {
        Value = value;
    }

    public static Result<TripTitle> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<TripTitle>.Fail(ResultErrors.Validation("Title is required.", nameof(TripTitle)));

        var normalized = input.Trim();
        if (normalized.Length > MaxLength)
            return Result<TripTitle>.Fail(ResultErrors.Validation($"Title must be at most {MaxLength} characters.", nameof(TripTitle)));

        return Result<TripTitle>.Ok(new TripTitle(normalized));
    }

    public static TripTitle From(string input)
    {
        var result = Create(input);
        if (result.IsSuccess && result.Value is not null)
            return result.Value;

        throw new InvalidOperationException(result.Errors.FirstOrDefault().Message ?? "Invalid title.");
    }
}

