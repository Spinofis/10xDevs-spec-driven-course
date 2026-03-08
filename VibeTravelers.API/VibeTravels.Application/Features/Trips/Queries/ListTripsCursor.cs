using System.Text;
using System.Text.Json;

namespace VibeTravels.Application.Features.Trips.Queries;

internal static class ListTripsCursor
{
    internal enum SortField
    {
        CreatedAt,
        GeneratedAt,
        Title
    }

    internal readonly record struct SortSpec(SortField Field, bool Desc);

    internal sealed record Payload(
        int V,
        SortField Field,
        bool Desc,
        string? LastValue,
        Guid LastId,
        bool? LastIsNull);

    internal static SortSpec ParseSortOrDefault(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return new SortSpec(SortField.CreatedAt, Desc: true);

        var trimmed = sort.Trim();
        var desc = trimmed.StartsWith('-');
        var fieldText = desc ? trimmed[1..] : trimmed;

        return fieldText switch
        {
            "createdAt" => new SortSpec(SortField.CreatedAt, desc),
            "generatedAt" => new SortSpec(SortField.GeneratedAt, desc),
            "title" => new SortSpec(SortField.Title, desc),
            _ => throw new InvalidOperationException("Invalid sort field.")
        };
    }

    internal static bool TryDecode(string? cursor, out Payload payload)
    {
        payload = default!;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(cursor.Trim()));
            var parsed = JsonSerializer.Deserialize<Payload>(json);
            if (parsed is null)
                return false;

            if (parsed.V != 1)
                return false;

            if (parsed.LastId == Guid.Empty)
                return false;

            payload = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static string Encode(SortSpec sort, string? lastValue, Guid lastId, bool? lastIsNull)
    {
        var payload = new Payload(
            V: 1,
            Field: sort.Field,
            Desc: sort.Desc,
            LastValue: lastValue,
            LastId: lastId,
            LastIsNull: lastIsNull);

        var json = JsonSerializer.Serialize(payload);
        return Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 0:
                break;
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
            default:
                throw new FormatException("Invalid base64url length.");
        }

        return Convert.FromBase64String(padded);
    }
}

