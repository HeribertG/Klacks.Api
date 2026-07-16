// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects skills whose distinctive trigger phrases appear verbatim in a message that a recipe's
/// keyword trigger has just matched, signalling that the deterministic recipe hijack may misroute a
/// skill-intent (e.g. a company-rule intake mentioning shifts). A phrase only competes when the
/// matched recipe trigger does not itself own it, so clean recipe requests keep the silent fast path.
/// </summary>
/// <param name="message">The user chat message the recipe trigger matched.</param>
/// <param name="language">The detected message language (selects per-language skill synonyms).</param>
/// <param name="matchedTrigger">The trigger of the recipe that keyword-matched the message.</param>
/// <param name="matchedRecipeSynonyms">The matched recipe's synonyms for the detected language, if any.</param>
/// <param name="servedSkillNames">Skill names the recipe executes itself; they never compete.</param>

using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ICompetingSkillIntentDetector
{
    Task<IReadOnlyList<string>> FindCompetingSkillNamesAsync(
        string message,
        string? language,
        RecipeTrigger matchedTrigger,
        IReadOnlyCollection<string>? matchedRecipeSynonyms,
        IReadOnlyCollection<string> servedSkillNames,
        CancellationToken cancellationToken = default);
}
