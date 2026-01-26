using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Common;

[Owned]
public class MultiLanguage
{
    private static readonly Lazy<string[]> _supportedLanguages = new(() =>
        typeof(MultiLanguage)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.Name.ToLowerInvariant())
            .ToArray());

    public static string[] SupportedLanguages => _supportedLanguages.Value;

    public string? De { get; set; }

    public string? En { get; set; }

    public string? Fr { get; set; }

    public string? It { get; set; }

    public string? GetValue(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "de" => De,
            "en" => En,
            "fr" => Fr,
            "it" => It,
            _ => null
        };
    }

    public void SetValue(string language, string? value)
    {
        switch (language.ToLowerInvariant())
        {
            case "de": De = value; break;
            case "en": En = value; break;
            case "fr": Fr = value; break;
            case "it": It = value; break;
        }
    }

    public Dictionary<string, string> ToDictionary()
    {
        var result = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(De))
        {
            result["de"] = De;
        }

        if (!string.IsNullOrEmpty(En))
        {
            result["en"] = En;
        }

        if (!string.IsNullOrEmpty(Fr))
        {
            result["fr"] = Fr;
        }

        if (!string.IsNullOrEmpty(It))
        {
            result["it"] = It;
        }

        return result;
    }

    public bool IsEmpty => string.IsNullOrEmpty(De) &&
                          string.IsNullOrEmpty(En) &&
                          string.IsNullOrEmpty(Fr) &&
                          string.IsNullOrEmpty(It);

    public static MultiLanguage Empty() => new();
}
