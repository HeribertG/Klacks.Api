// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the base language out of a BCP-47 language tag, the operation every language-aware
/// lookup in Klacks needs before it can fall back from a regional tag to the language itself, and tells
/// whether a language is written right to left. Callers that keep a region-specific entry (such as "zh-CN")
/// must probe their own key set with the full tag first, and callers that need a case-insensitive key do
/// their own lower-casing, so BaseLanguage never silently changes a lookup that was ordinal.
/// </summary>

namespace Klacks.Api.Domain.Common;

public static class LanguageTag
{
    private const char RegionSeparator = '-';

    private static readonly HashSet<string> RightToLeftLanguages = new(["ar", "he"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the base language of a tag is written right to left; true for the shipped Arabic and Hebrew packs.
    /// </summary>
    /// <param name="tag">BCP-47 language tag, possibly regional; null or blank is not right to left</param>
    public static bool IsRightToLeft(string? tag)
    {
        var baseLanguage = BaseLanguage(tag);
        return baseLanguage != null && RightToLeftLanguages.Contains(baseLanguage);
    }

    /// <summary>
    /// Returns the base language of a language tag (the part before the region separator); null for a blank tag.
    /// </summary>
    /// <param name="tag">BCP-47 language tag such as "de", "en-GB" or "zh-CN"; null or blank yields
    /// null, a tag without a region is returned trimmed and unchanged.</param>
    public static string? BaseLanguage(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var trimmed = tag.Trim();
        var separatorIndex = trimmed.IndexOf(RegionSeparator);
        return separatorIndex > 0 ? trimmed[..separatorIndex] : trimmed;
    }
}
