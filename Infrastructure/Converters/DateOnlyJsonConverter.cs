// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads a required DateOnly from the wire and writes it back as yyyy-MM-dd. Parsing is delegated to
/// DateOnlyStringParser so this converter and DateOnlyNullableJsonConverter never disagree about which
/// strings are valid or which calendar day a date/time with a UTC offset resolves to.
/// </summary>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Converters;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(DateOnlyStringParser.InvalidFormatMessage);
        }

        var s = reader.GetString();
        if (string.IsNullOrEmpty(s))
        {
            throw new JsonException(DateOnlyStringParser.InvalidFormatMessage);
        }

        return DateOnlyStringParser.Parse(ref reader, s);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateOnly value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}
