// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// System.Text.Json Converter für MultiLanguage.
/// Serialisiert/deserialisiert alle Sprachen (Core + Plugin) dynamisch als flaches JSON-Objekt.
/// Ersetzt den [JsonExtensionData]-Ansatz, der bei der Deserialisierung Plugin-Sprachen verliert.
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
