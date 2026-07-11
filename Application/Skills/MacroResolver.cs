// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the macro skills: resolves a calculation macro from a user-supplied name
/// via the staged NameResolution matcher. Spoken or STT-damaged queries like "Makro-All-Shift"
/// or "all shift" still resolve the camel-case macro "AllShift": the type-label words
/// ("makro"/"macro") are stripped and the compact stage bridges the missing word boundaries.
/// When several macros remain plausible the resolver returns a disambiguation error listing
/// them instead of silently picking one; an unknown name reports the real macro names.
/// </summary>

using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.Api.Application.Skills;

internal static class MacroResolver
{
    private static readonly string[] LabelWords = ["makro", "makros", "macro", "macros"];

    public static (MacroResource? Macro, string? Error) Resolve(
        IReadOnlyList<MacroResource> macros, string? macroName)
    {
        var named = macros
            .Where(m => !string.IsNullOrWhiteSpace(m.Name))
            .ToList();
        var query = (macroName ?? string.Empty).Trim();

        var resolution = NameResolution.Resolve(named, m => m.Name, query, LabelWords);
        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Candidates.Count > 1)
        {
            return (null,
                $"The macro name '{query}' is ambiguous — it matches several macros: " +
                string.Join(", ", resolution.Candidates.Select(m => $"{m.Name} ({m.Id})")) + ". " +
                "Ask the user which exact macro they mean — do not guess.");
        }

        var available = named.Count > 0
            ? "Available macros: " + string.Join(", ", named.Select(m => m.Name)) + "."
            : "There are no macros yet.";
        return (null,
            $"Macro '{query}' not found. {available} " +
            "Do not call this skill again with the same macro name — pick the correct name from " +
            "this list or ask the user. Offer the user only these real macro names — do not invent macros.");
    }
}
