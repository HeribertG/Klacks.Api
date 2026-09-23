// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Keeps the synonyms a language pack contributes in skill_phrase and in the legacy jsonb dictionary of a
/// skill or recipe in step. Used by the skill- and recipe-synonym install, uninstall and backfill paths of
/// LanguagePluginContentInstaller, so every path merges and replaces pack phrases the same way.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Settings;

internal static class LanguagePackPhraseMirror
{
    /// <summary>
    /// Builds the new jsonb value of one language: the pack phrases first, followed by every entry of
    /// that language that did not come from the pack's previous install.
    /// </summary>
    /// <param name="phraseRepo">Repository reading the pack's previous skill_phrase rows</param>
    /// <param name="ownerKind">Skill or Recipe, see SkillPhraseOwnerKinds</param>
    /// <param name="ownerName">Business name of the skill or recipe</param>
    /// <param name="code">Language code of the plugin being installed or uninstalled</param>
    /// <param name="currentMirror">The jsonb value of that language as it is stored now</param>
    /// <param name="packPhrases">The pack's phrases; an empty list removes the pack's contribution</param>
    // The legacy jsonb value of a language is not only the pack: an admin edit on the learning card
    // (UpdateLearnedCapabilityCommandHandler) writes it directly, without a skill_phrase row. So a pack
    // install or uninstall replaces exactly the pack's previous phrases - read from skill_phrase before
    // ReplacePackPhrasesAsync overwrites them - and keeps every other entry of that language.
    internal static async Task<List<string>> MergePackIntoMirrorAsync(
        ISkillPhraseRepository phraseRepo,
        string ownerKind,
        string ownerName,
        string code,
        IReadOnlyList<string>? currentMirror,
        IReadOnlyList<string> packPhrases)
    {
        var previousPack = await phraseRepo.GetPhraseTextsBySourceAsync(
            ownerKind, ownerName, SkillPhraseKinds.Synonym, SkillPhraseSources.LanguagePack, code);
        var previousPackSet = new HashSet<string>(previousPack, StringComparer.Ordinal);
        var retained = (currentMirror ?? []).Where(phrase => !previousPackSet.Contains(phrase));

        return packPhrases.Concat(retained).Distinct(StringComparer.Ordinal).ToList();
    }

    internal static void SetOrRemove(Dictionary<string, List<string>> mirror, string code, List<string> phrases)
    {
        if (phrases.Count == 0)
        {
            mirror.Remove(code);
            return;
        }

        mirror[code] = phrases;
    }

    /// <summary>
    /// Writes the synonyms a language pack contributes into skill_phrase next to the legacy jsonb
    /// dictionary. The replacement is restricted to the LanguagePack origin of exactly this language
    /// code, so installing or removing a pack can neither delete the seeded core-language phrases nor
    /// those of another installed pack. The code is used verbatim as the language key, because the
    /// jsonb dictionary is keyed the same way and the two must stay comparable.
    /// </summary>
    /// <param name="phraseRepo">Repository writing the skill_phrase rows</param>
    /// <param name="ownerKind">Skill or Recipe, see SkillPhraseOwnerKinds</param>
    /// <param name="ownerName">Business name of the skill or recipe</param>
    /// <param name="code">Language code of the plugin being installed or uninstalled</param>
    /// <param name="synonyms">The synonyms of that language; an empty list removes them</param>
    internal static async Task ReplacePackPhrasesAsync(
        ISkillPhraseRepository phraseRepo,
        string ownerKind,
        string ownerName,
        string code,
        IReadOnlyList<string> synonyms)
    {
        await phraseRepo.ReplaceForLanguageAsync(
            ownerKind,
            ownerName,
            SkillPhraseKinds.Synonym,
            SkillPhraseSources.LanguagePack,
            code,
            synonyms);
    }
}
