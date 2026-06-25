using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financarias.Integrations.Common.Json;

/// <summary>
///     Reads a nullable boolean that an external API may send either as a JSON boolean
///     (true/false) or as a string ("true"/"false"). ViaCEP, for instance, returns
///     { "erro": "true" } as a string for a valid-but-nonexistent CEP.
/// </summary>
public sealed class FlexibleBooleanConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.String => bool.TryParse(reader.GetString(), out var value) ? value : null,
            _ => throw new JsonException($"Cannot convert a token of type {reader.TokenType} to bool?."),
        };
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteBooleanValue(value.Value);
        }
    }
}