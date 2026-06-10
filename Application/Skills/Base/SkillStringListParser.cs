// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses a skill parameter into a list of strings. Accepts JSON arrays, delimited text
/// (comma, semicolon or newline separated) and already-deserialized enumerables.
/// </summary>
/// <param name="parameters">Raw skill parameter dictionary</param>
/// <param name="name">Name of the parameter to parse</param>
using System.Text.Json;

namespace Klacks.Api.Application.Skills.Base;

public static class SkillStringListParser
{
    private static readonly char[] TextSeparators = [',', ';', '\n'];
    private const char JsonArrayStartChar = '[';

    public static List<string>? ParseStringList(Dictionary<string, object> parameters, string name)
    {
        if (!parameters.TryGetValue(name, out var raw) || raw == null)
        {
            return null;
        }

        return raw switch
        {
            JsonElement { ValueKind: JsonValueKind.Array } jsonArray => FromJsonArray(jsonArray),
            JsonElement jsonValue => FromText(jsonValue.ToString()),
            string text => FromText(text),
            IEnumerable<object> items => FromEnumerable(items),
            _ => FromText(raw.ToString() ?? string.Empty)
        };
    }

    private static List<string> FromJsonArray(JsonElement jsonArray)
    {
        return jsonArray.EnumerateArray()
            .Select(element => element.ToString().Trim())
            .Where(value => value.Length > 0)
            .ToList();
    }

    private static List<string> FromEnumerable(IEnumerable<object> items)
    {
        return items
            .Select(item => item?.ToString()?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .ToList();
    }

    private static List<string> FromText(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length > 0 && trimmed[0] == JsonArrayStartChar)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(trimmed);
                if (parsed != null)
                {
                    return parsed
                        .Select(value => value.Trim())
                        .Where(value => value.Length > 0)
                        .ToList();
                }
            }
            catch (JsonException)
            {
            }
        }

        return trimmed
            .Split(TextSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => value.Length > 0)
            .ToList();
    }
}
