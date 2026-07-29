// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Assistant;

/// <summary>
/// Reads the two shapes that agent_skills.trigger_keywords can have. The column historically held a
/// flat JSON array without any language information; it now holds an object keyed by language, the
/// same shape agent_skills.synonyms already used. Both are accepted so a partially migrated database
/// keeps working: a flat array is read as a single Undetermined group.
/// </summary>
public static class TriggerKeywordFormat
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Matches SkillSeedLoader.JsonWriteOptions so the seed path and the administrator path write the
    // column identically. The keys are ASCII language tags, which camel casing leaves untouched.
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Returns every phrase regardless of language, in storage order. Used by the matching paths,
    /// which search all languages anyway.
    /// </summary>
    /// <param name="json">Raw column value, either a JSON array or a JSON object keyed by language</param>
    public static IReadOnlyList<string> ReadFlat(string? json)
    {
        var groups = ReadGrouped(json);
        if (groups.Count == 0)
        {
            return [];
        }

        return groups.SelectMany(g => g.Value).ToList();
    }

    /// <summary>
    /// Returns the phrases grouped by language. A legacy flat array yields a single group under
    /// SkillPhraseLanguages.Undetermined rather than an empty result, so no phrase is ever dropped.
    /// </summary>
    /// <param name="json">Raw column value, either a JSON array or a JSON object keyed by language</param>
    public static IReadOnlyDictionary<string, List<string>> ReadGrouped(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]" || json == "{}")
        {
            return new Dictionary<string, List<string>>();
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                var flat = JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
                return flat.Count == 0
                    ? new Dictionary<string, List<string>>()
                    : new Dictionary<string, List<string>> { [SkillPhraseLanguages.Undetermined] = flat };
            }

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions)
                       ?? new Dictionary<string, List<string>>();
            }
        }
        catch (JsonException)
        {
            return new Dictionary<string, List<string>>();
        }

        return new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Serializes grouped phrases into the column format using the shared write options, so every
    /// writer produces byte-identical JSON for the same input.
    /// </summary>
    /// <param name="groups">Phrases keyed by language tag, or by the reserved mul/und keys</param>
    public static string Write(IReadOnlyDictionary<string, List<string>>? groups) =>
        Write(groups, WriteOptions);

    /// <summary>
    /// Serializes grouped phrases back into the column format. Empty groups are dropped so an empty
    /// input always produces "{}" rather than a set of empty arrays.
    /// </summary>
    /// <param name="groups">Phrases keyed by language tag, or by the reserved mul/und keys</param>
    /// <param name="options">Serializer options of the caller, so escaping stays consistent</param>
    public static string Write(IReadOnlyDictionary<string, List<string>>? groups, JsonSerializerOptions options)
    {
        if (groups == null || groups.Count == 0)
        {
            return "{}";
        }

        var nonEmpty = groups
            .Where(g => g.Value is { Count: > 0 })
            .ToDictionary(g => g.Key, g => g.Value);

        return nonEmpty.Count == 0 ? "{}" : JsonSerializer.Serialize(nonEmpty, options);
    }
}
