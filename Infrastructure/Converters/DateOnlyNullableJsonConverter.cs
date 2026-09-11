// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads an optional DateOnly from the wire and writes it back as yyyy-MM-dd. Null and a blank string
/// are read as no value; a non-blank string that cannot be understood throws instead of silently
/// becoming null. Parsing itself is delegated to DateOnlyStringParser so this converter and
/// DateOnlyJsonConverter never disagree about which strings are valid.
/// </summary>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Converters;

public class DateOnlyNullableJsonConverter : JsonConverter<DateOnly?>
{
    public override DateOnly? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(DateOnlyStringParser.InvalidFormatMessage);
        }

        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        return DateOnlyStringParser.Parse(ref reader, s);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateOnly? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
