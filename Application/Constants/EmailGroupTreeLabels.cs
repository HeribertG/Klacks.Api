// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Localized labels for the synthetic "Unassigned" container in the email group tree.
/// The label is resolved from an optional ISO 639-1 language code (de/en/fr/it); unknown or missing codes fall back to English.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class EmailGroupTreeLabels
{
    public const string UnassignedDe = "Nicht zugeordnet";
    public const string UnassignedEn = "Unassigned";
    public const string UnassignedFr = "Non assigné";
    public const string UnassignedIt = "Non assegnato";

    private const int LanguageCodeLength = 2;

    public static string GetUnassignedLabel(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return UnassignedEn;
        }

        var code = language.Trim();
        if (code.Length > LanguageCodeLength)
        {
            code = code[..LanguageCodeLength];
        }

        return code.ToLowerInvariant() switch
        {
            "de" => UnassignedDe,
            "fr" => UnassignedFr,
            "it" => UnassignedIt,
            _ => UnassignedEn,
        };
    }
}
