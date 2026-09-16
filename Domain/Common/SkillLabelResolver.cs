// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the user-facing label of a skill for one language. Sits next to LanguageTag because it is the
/// same kind of primitive, but it deliberately does NOT reduce to the base language first: the label
/// dictionary is keyed by pack code, and two of the 21 packs Klacks ships carry a region (zh-CN, zh-TW).
/// Stripping the region before the lookup would make the two Chinese scripts share one entry, which is the
/// defect LanguageDirectiveCoverageGuardTests already names for the answer-language directives. The full
/// tag therefore wins, and only then does the base language act as a fallback - which is what lets a
/// de-CH installation read the authored "de" label.
/// There is no English fallback anywhere in this class. A language nobody authored a label for yields
/// null, and the correction path then asks no question at all (spec §1 rule 4: one language per user, all
/// 25, no English stop-gap). Returning English here would put English nouns into a translated frame,
/// which is exactly the substance violation the interim gate in CorrectionOutcomeComposer was holding
/// the line against.
/// </summary>

namespace Klacks.Api.Domain.Common;

public static class SkillLabelResolver
{
    /// <param name="labels">Authored labels of one skill, keyed by language tag; null when the skill has none</param>
    /// <param name="language">Active language tag of the turn, such as "de", "de-CH" or "zh-TW"</param>
    public static string? Resolve(IReadOnlyDictionary<string, string>? labels, string? language)
    {
        if (labels == null || labels.Count == 0 || string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var tag = language!.Trim();

        if (TryTake(labels, tag, out var exact))
        {
            return exact;
        }

        var baseLanguage = LanguageTag.BaseLanguage(tag);

        return baseLanguage != null
               && !string.Equals(baseLanguage, tag, StringComparison.OrdinalIgnoreCase)
               && TryTake(labels, baseLanguage, out var fallback)
            ? fallback
            : null;
    }

    /// <summary>
    /// The same lookup, cut to a maximum length. A separate overload rather than an optional parameter,
    /// because an optional parameter appended to an existing signature rebinds positional arguments
    /// without failing to compile (the 2026-09-14 overload trap ISkillToolsetAssembler documents).
    /// Both consumers of a label cap it - the clarification's two option slots and its previous-action
    /// slot - and they keep their own, deliberately different constants, so the length stays the
    /// caller's decision while the cutting rule lives in one place. The cut end is trimmed: a slice
    /// through a word gap would otherwise leave the label ending in whitespace.
    /// </summary>
    /// <param name="labels">Authored labels of one skill, keyed by language tag; null when the skill has none</param>
    /// <param name="language">Active language tag of the turn, such as "de", "de-CH" or "zh-TW"</param>
    /// <param name="maxLength">Maximum number of characters the caller's slot can carry</param>
    public static string? Resolve(
        IReadOnlyDictionary<string, string>? labels, string? language, int maxLength)
    {
        var label = Resolve(labels, language);

        return label == null || label.Length <= maxLength ? label : label[..maxLength].TrimEnd();
    }

    private static bool TryTake(IReadOnlyDictionary<string, string> labels, string key, out string? label)
    {
        label = null;

        foreach (var entry in labels)
        {
            if (!string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(entry.Value))
            {
                continue;
            }

            label = entry.Value.Trim();
            return true;
        }

        return false;
    }
}
