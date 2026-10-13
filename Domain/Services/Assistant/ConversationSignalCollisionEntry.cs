// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One word (or phrase) that some languages list as a yes and others as a no, e.g. Polish "tak" (yes) against
/// Malay "tak" (no). ResolveFor decides which reading applies for a UI language: a language that lists the word on
/// one side only settles it (Affirming or Negating). A language that lists the word on neither side as an entry of its
/// own but uses it inside its own phrases (Danish "nej tak", "ikke nu, tak") is Neutral: there the bare word is no
/// yes/no signal at all and the rest of the message decides. Any other language, and a language that lists the word on
/// both sides, leaves it Ambiguous.
/// </summary>
/// <param name="Word">The colliding entry, trimmed, NFC-normalized and lowercase</param>
/// <param name="AffirmingLanguages">Languages that list the word among their affirmations, sorted</param>
/// <param name="NegatingLanguages">Languages that list the word in any negating list, sorted</param>
/// <param name="NegatingCategories">The negating lists that carry the word, sorted</param>
/// <param name="NeutralLanguages">Languages that use the word only inside their own multi-word phrases, never as an entry of its own, sorted</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed record ConversationSignalCollisionEntry(
    string Word,
    IReadOnlyList<string> AffirmingLanguages,
    IReadOnlyList<string> NegatingLanguages,
    IReadOnlyList<string> NegatingCategories,
    IReadOnlyList<string> NeutralLanguages)
{
    public IReadOnlyList<string> AllLanguages =>
        AffirmingLanguages.Concat(NegatingLanguages).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(language => language, StringComparer.OrdinalIgnoreCase).ToList();

    public CollisionResolution ResolveFor(string? language)
    {
        var affirms = Lists(AffirmingLanguages, language);
        var negates = Lists(NegatingLanguages, language);

        if (affirms && !negates)
        {
            return CollisionResolution.Affirming;
        }

        if (negates && !affirms)
        {
            return CollisionResolution.Negating;
        }

        return !affirms && Lists(NeutralLanguages, language) ? CollisionResolution.Neutral : CollisionResolution.Ambiguous;
    }

    private static bool Lists(IReadOnlyList<string> members, string? language)
    {
        var tag = language?.Trim();
        if (string.IsNullOrEmpty(tag))
        {
            return false;
        }

        var baseLanguage = LanguageTag.BaseLanguage(tag);
        return members.Any(member =>
            string.Equals(member, tag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(member, baseLanguage, StringComparison.OrdinalIgnoreCase));
    }
}
