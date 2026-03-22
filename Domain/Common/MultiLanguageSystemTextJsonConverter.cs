// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// System.Text.Json converter for MultiLanguage.
/// Serializes/deserializes all languages (core + plugin) dynamically as a flat JSON object.
/// Replaces the [JsonExtensionData] approach which loses plugin languages during deserialization.
/// </summary>
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Common;

public class MultiLanguageSystemTextJsonConverter : JsonConverter<MultiLanguage>
{
    public override MultiLanguage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var ml = new MultiLanguage();

        if (reader.TokenType != JsonTokenType.StartObject)
            return ml;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var key = reader.GetString()!;
            reader.Read();

            if (reader.TokenType == JsonTokenType.String)
            {
                ml.SetValue(key, reader.GetString());
            }
        }

        return ml;
    }

    public override void Write(Utf8JsonWriter writer, MultiLanguage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value.GetAllValues().OrderBy(x => x.Key))
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                writer.WriteString(kvp.Key, kvp.Value);
            }
        }

        writer.WriteEndObject();
    }
}
