using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Domain.ValueObjects;

public sealed record TripPlaceText
{
    public const int MaxLength = 300;

    public string Value { get; }

    private TripPlaceText(string value)
    {
        Value = value;
    }

    public static Result<TripPlaceText> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<TripPlaceText>.Fail(ResultErrors.Validation("PlaceText is required.", nameof(TripPlaceText)));

        var normalized = input.Trim();
        if (normalized.Length > MaxLength)
            return Result<TripPlaceText>.Fail(ResultErrors.Validation($"PlaceText must be at most {MaxLength} characters.", nameof(TripPlaceText)));

        return Result<TripPlaceText>.Ok(new TripPlaceText(normalized));
    }

    public static TripPlaceText From(string input)
    {
        var result = Create(input);
        if (result.IsSuccess && result.Value is not null)
            return result.Value;

        throw new InvalidOperationException(result.Errors.FirstOrDefault().Message ?? "Invalid place text.");
    }
}

