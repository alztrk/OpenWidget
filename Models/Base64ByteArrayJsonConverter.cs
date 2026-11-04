using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenWidget.Models;

public sealed class Base64ByteArrayJsonConverter : JsonConverter<byte[]?>
{
    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String && reader.TryGetBytesFromBase64(out var value))
        {
            return value;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return null;
        }

        throw new JsonException("Clipboard image data must be a base64 string or null.");
    }

    public override void Write(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteBase64StringValue(value);
    }
}
