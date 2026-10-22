// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The 25 base languages the local Parakeet engine can transcribe. Mirrors Klacks.Stt ParakeetLanguages (the
/// service enforces the same set and rejects other languages), so the install default, the plugin status and
/// the skill texts can tell an installation whether its language is covered without asking the service.
/// A region suffix is ignored (de-CH counts as de); a blank language counts as covered because the engine then
/// detects the language itself.
/// </summary>
namespace Klacks.Api.Application.Constants;

using Klacks.Api.Domain.Common;

public static class ParakeetLanguageCoverage
{
    public static readonly IReadOnlyList<string> Languages = Array.AsReadOnly(
    [
        "bg", "cs", "da", "de", "el", "en", "es", "et", "fi", "fr", "hr", "hu", "it",
        "lt", "lv", "mt", "nl", "pl", "pt", "ro", "ru", "sk", "sl", "sv", "uk",
    ]);

    private static readonly HashSet<string> Covered = new(Languages, StringComparer.OrdinalIgnoreCase);

    public static bool IsCovered(string? language)
    {
        var baseLanguage = LanguageTag.BaseLanguage(language);
        return baseLanguage is null || Covered.Contains(baseLanguage);
    }
}
