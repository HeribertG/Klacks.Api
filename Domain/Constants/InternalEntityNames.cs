// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Internal technical entity names that must never surface in user-facing assistant content,
/// each paired with the user-facing term that replaces it. Used both to guard auto-extracted
/// memories (so curated user terminology cannot be displaced by leaked internal names) and to
/// sanitize retrieved knowledge content before it reaches the assistant.
/// </summary>
/// <param name="text">Text to scan; matched case-insensitively against all internal names</param>
namespace Klacks.Api.Domain.Constants;

public static class InternalEntityNames
{
    public static readonly IReadOnlyList<(string Name, string UserTerm)> Replacements =
    [
        ("OriginalOrder", "Bestellung"),
        ("SealedOrder", "versiegelte Bestellung"),
        ("OriginalShift", "planbare Schicht"),
        ("SplitShift", "Teilstück"),
        ("BreakPlaceholder", "vorgeplante Absenz"),
        ("Break-Placeholder", "vorgeplante Absenz"),
        ("AnalyseToken", "Analyse-Szenario"),
        ("DayLock", "Tagessperre"),
        ("WorkChange", "Arbeitskorrektur")
    ];

    public static readonly IReadOnlyList<string> All =
        Replacements.Select(r => r.Name).ToList();

    public static bool ContainsAny(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return All.Any(name => text.Contains(name, StringComparison.OrdinalIgnoreCase));
    }
}
