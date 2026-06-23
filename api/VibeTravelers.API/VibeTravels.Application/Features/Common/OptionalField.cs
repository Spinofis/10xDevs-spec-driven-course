using System.Text.Json;
using System.Text.Json.Serialization;

namespace VibeTravels.Application.Features.Common;

[JsonConverter(typeof(OptionalFieldJsonConverterFactory))]
public readonly struct OptionalField<T>
{
    private readonly T _value = default!;

    private OptionalField(bool isSet, T value)
    {
        IsSet = isSet;
        _value = value;
    }

    public bool IsSet { get; }

    public T Value => IsSet
        ? _value
        : throw new InvalidOperationException("Optional field is not set.");

    public static OptionalField<T> Unset() => default;
    public static OptionalField<T> FromValue(T value) => new(true, value);
}

public sealed class OptionalFieldJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(OptionalField<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalFieldJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class OptionalFieldJsonConverter<TValue> : JsonConverter<OptionalField<TValue>>
    {
        public override OptionalField<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (default(TValue) is null)
                    return OptionalField<TValue>.FromValue(default!);

                throw new JsonException($"Null is not allowed for '{typeof(TValue).Name}'.");
            }

            var value = JsonSerializer.Deserialize<TValue>(ref reader, options);
            return OptionalField<TValue>.FromValue(value!);
        }

        public override void Write(Utf8JsonWriter writer, OptionalField<TValue> value, JsonSerializerOptions options)
        {
            if (value.IsSet is false)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
