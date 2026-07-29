// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Persistence.Seed.Models;

/// <summary>
/// Reads skill-seeds.json triggerKeywords in either shape: the historical flat array, or the object
/// keyed by language that replaced it. A flat array is read as a single Undetermined group, so an
/// unmigrated seed file still loads and its phrases keep working in every language.
/// </summary>
public sealed class TriggerKeywordGroupsConverter : JsonConverter<Dictionary<string, List<string>>>
{
    public override Dictionary<string, List<string>>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var flat = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
            return flat.Count == 0
                ? []
                : new Dictionary<string, List<string>> { [SkillPhraseLanguages.Undetermined] = flat };
        }

        return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(ref reader, options) ?? [];
    }

    public override void Write(
        Utf8JsonWriter writer, Dictionary<string, List<string>> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}
