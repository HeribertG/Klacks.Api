// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Common;

[Owned]
public class MultiLanguage
{
    private Dictionary<string, string?> _values = new(StringComparer.OrdinalIgnoreCase);

    public static string[] CoreLanguages => ["de", "en", "fr", "it"];

    [Obsolete("Use CoreLanguages or LanguageConfig.SupportedLanguages instead")]
    public static string[] SupportedLanguages => CoreLanguages;

    [JsonPropertyName("de")]
    public string? De
    {
        get => GetValue("de");
        set => SetValue("de", value);
    }

    [JsonPropertyName("en")]
    public string? En
    {
        get => GetValue("en");
        set => SetValue("en", value);
    }

    [JsonPropertyName("fr")]
    public string? Fr
    {
        get => GetValue("fr");
        set => SetValue("fr", value);
    }

    [JsonPropertyName("it")]
    public string? It
    {
        get => GetValue("it");
        set => SetValue("it", value);
    }

    [JsonExtensionData]
    [NotMapped]
    public Dictionary<string, object?> AdditionalLanguages
    {
        get => _values
            .Where(kvp => !CoreLanguages.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
        set
        {
            foreach (var kvp in value)
            {
                if (!CoreLanguages.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                {
                    SetValue(kvp.Key, kvp.Value?.ToString());
                }
            }
        }
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
