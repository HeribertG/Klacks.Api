// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runtime safety net against recipe over-matching: after a recipe's keyword trigger matched a
/// message, this detector looks for skills whose multiword trigger phrases (keywords or synonyms of
/// the detected language) appear verbatim in the same message. A phrase competes only when the
/// matched recipe trigger does not fully match the phrase on its own — a phrase the trigger owns
/// (e.g. "delete absence" against the remove-absence recipe) is the recipe's legitimate territory,
/// while a foreign phrase (e.g. "neue firmenregel" against the create-shift-order recipe) means the
/// recipe fired on incidental vocabulary elsewhere in the message and must not hijack the turn
/// silently. Recipe step skills never compete: the recipe is their guided flow.
/// </summary>
/// <param name="skillRepository">Source of the enabled skills whose trigger phrases are scanned.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class CompetingSkillIntentDetector : ICompetingSkillIntentDetector
{
    private readonly IAgentSkillRepository _skillRepository;

    public CompetingSkillIntentDetector(IAgentSkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<IReadOnlyList<string>> FindCompetingSkillNamesAsync(
        string message,
        string? language,
        RecipeTrigger matchedTrigger,
        IReadOnlyCollection<string>? matchedRecipeSynonyms,
        IReadOnlyCollection<string> servedSkillNames,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return [];
        }

        var skills = await _skillRepository.GetAllEnabledAsync(cancellationToken);
        return FindCompetingSkillNames(skills, message, language, matchedTrigger, matchedRecipeSynonyms, servedSkillNames);
    }

    public static IReadOnlyList<string> FindCompetingSkillNames(
        IEnumerable<AgentSkill> skills,
        string message,
        string? language,
        RecipeTrigger matchedTrigger,
        IReadOnlyCollection<string>? matchedRecipeSynonyms,
        IReadOnlyCollection<string> servedSkillNames)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return [];
        }

        var served = new HashSet<string>(servedSkillNames, StringComparer.OrdinalIgnoreCase);
        var competing = new List<string>();

        foreach (var skill in skills)
        {
            if (served.Contains(skill.Name))
            {
                continue;
            }

            var matchedPhrases = SkillMatchingEngine.MatchedMultiwordPhrases(skill, message, language);
            if (matchedPhrases.Any(phrase => !RecipeTriggerMatcher.Matches(matchedTrigger, matchedRecipeSynonyms, phrase)))
            {
                competing.Add(skill.Name);
            }
        }

        return competing;
    }
}
