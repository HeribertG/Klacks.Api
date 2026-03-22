// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Multilingual value object for JSONB columns.
/// Core languages (de/en/fr/it) have explicit properties for code access.
/// All languages (core + plugin) are serialized dynamically via MultiLanguageSystemTextJsonConverter.
/// </summary>
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Common;

[JsonConverter(typeof(MultiLanguageSystemTextJsonConverter))]
public class MultiLanguage
{
    private Dictionary<string, string?> _values = new(StringComparer.OrdinalIgnoreCase);

    public static string[] CoreLanguages => ["de", "en", "fr", "it"];

    [Obsolete("Use CoreLanguages or LanguageConfig.SupportedLanguages instead")]
    public static string[] SupportedLanguages => CoreLanguages;

    [JsonIgnore]
    public string? De
    {
        get => GetValue("de");
        set => SetValue("de", value);
    }

    [JsonIgnore]
    public string? En
    {
        get => GetValue("en");
        set => SetValue("en", value);
    }

    [JsonIgnore]
    public string? Fr
    {
        get => GetValue("fr");
        set => SetValue("fr", value);
    }

    [JsonIgnore]
    public string? It
    {
        get => GetValue("it");
        set => SetValue("it", value);
    }

    public string? GetValue(string language)
    {
        return _values.GetValueOrDefault(language.ToLowerInvariant());
    }

    public void SetValue(string language, string? value)
    {
        var key = language.ToLowerInvariant();
        if (string.IsNullOrEmpty(value))
        {
            _values.Remove(key);
        }
        else
        {
            _values[key] = value;
        }
    }

    public Dictionary<string, string> ToDictionary()
    {
        return _values
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);
    }

    [JsonIgnore]
    public bool IsEmpty => _values.Count == 0 || _values.Values.All(string.IsNullOrEmpty);

    public static MultiLanguage Empty() => new();

    public IEnumerable<string> GetPopulatedLanguages()
    {
        return _values
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .Select(kvp => kvp.Key);
    }

    public IReadOnlyDictionary<string, string?> GetAllValues() => _values;
}
