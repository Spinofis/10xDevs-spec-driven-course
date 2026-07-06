namespace VibeTravels.Application.Features.Common;

public static class EnumParsing
{
    public static TEnum? ParseNullable<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }
}
