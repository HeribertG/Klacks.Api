// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the base language out of a BCP-47 language tag, the one operation every language-aware
/// lookup in Klacks needs before it can fall back from a regional tag to the language itself.
/// Deliberately does nothing else: callers that keep a region-specific entry (such as "zh-CN") must
/// probe their own key set with the full tag first, and callers that need a case-insensitive key do
/// their own lower-casing, so this never silently changes a lookup that was ordinal.
/// </summary>

namespace Klacks.Api.Domain.Common;

public static class LanguageTag
{
    private const char RegionSeparator = '-';

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
