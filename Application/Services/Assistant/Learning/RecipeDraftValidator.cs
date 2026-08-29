// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The gate that decides whether a generated capability may become a row in agent_recipes. It exists
/// because the quality gates that keep hand-written recipes honest - RecipeSeedQualityTests and
/// SkillRecipeTriggerCrossQualityTests - read recipe-seeds.json, not the table: a recipe the loop writes
/// straight into the database passes none of them. The rules those tests enforce are therefore enforced
/// here instead, at runtime, before the insert.
/// The reason this has to happen before the insert rather than after is the recipe engine itself. A
/// recipe forces its step skill deterministically, ahead of any function calling, and the engine reads
/// the table on every single call with no cache. An enabled recipe is live on every instance the
/// instant it is written, so there is no window in which a bad trigger could be measured and withdrawn -
/// it would be stealing turns while being measured. Everything decidable without the database is
/// therefore decided first, with the trigger matcher the engine itself uses.
/// The two hijack directions are checked separately on purpose: that no existing intent falls into the
/// new recipe, and that the new recipe's own vocabulary does not fall into an existing one.
/// </summary>
/// <param name="skillRegistry">Supplies the trigger keywords a new recipe must not swallow</param>
/// <param name="logger">Reports the rule a draft broke</param>

using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.Domain.Services.Assistant;
using LanguageConstants = Klacks.Api.Application.Constants.LanguagePluginConstants;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class RecipeDraftValidator : IRecipeDraftValidator
{
    private const int MaxCanonicalSentences = 24;
    private const int MaxSkillPhrasesChecked = 4000;
    private const string Identifier = "[A-Za-z_][A-Za-z0-9_]*";

    private static readonly Regex SlugPattern =
        new("^[a-z][a-z0-9]*(-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Exactly what RecipeExecutionPlan.TryGetSlotName accepts: the prefix followed by an identifier and
    // nothing else. Anything after the identifier makes the whole remainder the slot name at runtime.
    private static readonly Regex SlotReferencePattern = new(
        "^" + Regex.Escape(RecipeEngineDefaults.SlotReferencePrefix) + Identifier + "$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Exactly what RecipeExecutionPlan.ExtractCapture can parse: an array property, the array marker, an
    // id property, the separator and the slot name.
    private static readonly Regex CaptureSpecPattern = new(
        "^" + Identifier + Regex.Escape(RecipeEngineDefaults.CaptureArrayMarker) + Identifier
        + Regex.Escape(RecipeEngineDefaults.CaptureSeparator) + Identifier + "$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Placeholder syntaxes the recipe engine does not know. They carry no prefix, so the runtime hands
    // them to the skill verbatim and nothing ever reports it.
    private static readonly Regex ForeignPlaceholderPattern =
        new(@"\{\{?\s*[A-Za-z_][A-Za-z0-9_]*\s*\}?\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions TriggerJsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private readonly ISkillRegistry _skillRegistry;
    private readonly ILogger<RecipeDraftValidator> _logger;

    public RecipeDraftValidator(ISkillRegistry skillRegistry, ILogger<RecipeDraftValidator> logger)
    {
        _skillRegistry = skillRegistry;
        _logger = logger;
    }

    public RecipeDraftVerdict Validate(
        LearnedRecipeDraft draft,
        IReadOnlyList<AgentRecipe> existingRecipes,
        IReadOnlyList<SkillLearningGoldenCase> goldenCases)
    {
        var shapeError = CheckShape(draft);
        if (shapeError != null)
        {
            return RecipeDraftVerdict.Rejected(shapeError);
        }

        var bindingError = CheckStepBindings(draft);
        if (bindingError != null)
        {
            return RecipeDraftVerdict.Rejected(bindingError);
        }

        var name = SkillLearningDefaults.LearnedRecipeNamePrefix + draft.Name;
        if (existingRecipes.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return RecipeDraftVerdict.Rejected($"A recipe named '{name}' already exists.");
        }

        var trigger = WithQuestionGuard(draft.Trigger, Mutates(draft));

        var hijackError = CheckHijack(trigger, existingRecipes, goldenCases);
        if (hijackError != null)
        {
            _logger.LogInformation("Rejected capability draft '{Name}': {Reason}", name, hijackError);
            return RecipeDraftVerdict.Rejected(hijackError);
        }

        return RecipeDraftVerdict.Accepted(name, trigger);
    }

    private static string? CheckShape(LearnedRecipeDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Name) || !SlugPattern.IsMatch(draft.Name))
        {
            return $"'{draft.Name}' is not a lower-case kebab-case slug.";
        }

        if (string.IsNullOrWhiteSpace(draft.Goal))
        {
            return "A capability needs a goal.";
        }

        var missing = LanguageConstants.CoreLanguages
            .Where(language => !draft.GoalTranslations.TryGetValue(language, out var text)
                || string.IsNullOrWhiteSpace(text))
            .ToList();

        if (missing.Count > 0)
        {
            return $"The goal is missing a translation for: {string.Join(", ", missing)}.";
        }

        if (draft.Trigger.AllOf.Count == 0)
        {
            return "A capability trigger needs at least one allOf condition.";
        }

        var shortTerm = TermsOf(draft.Trigger.AllOf)
            .FirstOrDefault(term => term.Trim().Length < SkillLearningDefaults.MinTriggerStemLength);

        return shortTerm == null
            ? null
            : $"Trigger term '{shortTerm}' is shorter than {SkillLearningDefaults.MinTriggerStemLength} "
              + "characters and would match unrelated words.";
    }

    // Two ways a draft can name a value the engine cannot produce, neither of which any later gate
    // catches on its own merits.
    // The first is a slot reference carrying a suffix. "$currentMonth-01" makes the runtime look up a
    // slot literally called "currentMonth-01"; nothing captures that, so GetParameterInjections drops the
    // parameter without a word and the step runs with whatever the model happened to pass. Oracle O2 does
    // reject it today, but only by accident and with the wrong reason - "no earlier step produces it" -
    // which sends the generator looking for a missing capture instead of dropping the suffix.
    // The second is worse because it passes everything. A value that merely LOOKS like a placeholder but
    // carries no prefix - "{{month}}-01" - is not a slot reference at all: the runtime treats it as a
    // literal constant and hands the braces to the skill verbatim, and O2's binding check counts it as a
    // constant and waves it through. That draft activates, and the raw text reaches a real user's turn.
    // The capture spec is checked for the same reason. ExtractCapture needs "array[].id as slot" and
    // silently yields nothing without the array marker, and a capture that yields nothing does not merely
    // fail: Observe() deactivates the whole recipe, leaving the user in a chat that stopped answering.
    // O2 only reads the part after " as ", so "items[].id as month" on a skill returning a flat object
    // registers a slot that can never be filled - the exact shape that would let a suffix-free rewrite of
    // the failing draft pass every gate and ship broken.
    private static string? CheckStepBindings(LearnedRecipeDraft draft)
    {
        foreach (var step in draft.Steps)
        {
            foreach (var (parameter, value) in step.Inject ?? [])
            {
                if (value != null
                    && value.Contains(RecipeEngineDefaults.SlotReferencePrefix, StringComparison.Ordinal)
                    && !SlotReferencePattern.IsMatch(value))
                {
                    return $"Step '{step.Skill}' binds '{parameter}' to '{value}'. A slot reference is the "
                        + "whole value and nothing else; it cannot be joined to any other text. Capture the "
                        + "finished value, or use a plain literal.";
                }

                if (value != null && ForeignPlaceholderPattern.IsMatch(value))
                {
                    return $"Step '{step.Skill}' binds '{parameter}' to '{value}', which looks like a "
                        + "placeholder but is passed to the skill verbatim. There is no template syntax: "
                        + "use a literal value, or a captured slot reference.";
                }
            }

            if (!string.IsNullOrWhiteSpace(step.Capture) && !CaptureSpecPattern.IsMatch(step.Capture))
            {
                return $"Step '{step.Skill}' captures with '{step.Capture}'. A capture reads one field out "
                    + "of a list the step returns and must be spelled 'array"
                    + RecipeEngineDefaults.CaptureArrayMarker + "field"
                    + RecipeEngineDefaults.CaptureSeparator + "slot'.";
            }
        }

        return null;
    }

    // The question guard is written in rather than demanded from the generator: a missing lead produces a
    // recipe that hijacks plain questions, and that is not a failure mode worth leaving to a model.
    // But it belongs only on a capability that WRITES. The guard exists because every hand-written
    // recipe is a guided mutation flow, where firing on "Welche Gruppen gibt es?" drags a plain question
    // into a confirmation gate. A capability composed purely of reading steps is the opposite: answering
    // a question is the whole point of it, and most such wishes are phrased as one.
    // Applied unconditionally it is self-defeating - the wish "welche mitarbeitenden haben im september
    // abwesenheiten" produces allOf ["welche…"] and noneOf startsWith ["welche"] in the same trigger, a
    // contradiction no utterance can satisfy. Two capabilities passed every other gate and executed all
    // their steps for real before the live engine refused to resolve them for exactly this reason.
    private static RecipeTrigger WithQuestionGuard(RecipeTrigger trigger, bool mutates)
    {
        var noneOf = new List<RecipeCondition>(trigger.NoneOf);

        if (mutates)
        {
            noneOf.Add(new RecipeCondition { StartsWith = [.. RecipeQuestionLeads.All] });
        }

        return new RecipeTrigger { AllOf = [.. trigger.AllOf], NoneOf = noneOf };
    }

    private static bool Mutates(LearnedRecipeDraft draft) =>
        draft.Steps.Any(step =>
            string.Equals(step.Kind, RecipeStepKinds.Mutate, StringComparison.OrdinalIgnoreCase));

    private string? CheckHijack(
        RecipeTrigger trigger,
        IReadOnlyList<AgentRecipe> existingRecipes,
        IReadOnlyList<SkillLearningGoldenCase> goldenCases)
    {
        foreach (var goldenCase in goldenCases)
        {
            if (RecipeTriggerMatcher.Matches(trigger, goldenCase.Query))
            {
                return $"It would swallow the utterance '{goldenCase.Query}', which must reach "
                       + $"'{goldenCase.ExpectedSourceId}'.";
            }
        }

        var skillPhraseError = CheckSkillPhrases(trigger);
        if (skillPhraseError != null)
        {
            return skillPhraseError;
        }

        // The two directions are not symmetric, and treating them alike breaks one of them.
        // Going out - would the DRAFT fire on an existing recipe's wording - the draft's exclusions are
        // dropped. The question guard was written in a few lines ago, several of its leads carry no
        // trailing space ("welche", "zeig", "liste"), and RecipeTriggerMatcher checks noneOf FIRST: a
        // probe sentence whose first word happens to begin with such a stem would veto itself and the
        // comparison would never happen. A probe sentence is a bag of vocabulary, not an utterance, so
        // an exclusion written for utterances has no business vetoing it.
        // Coming in - would an EXISTING recipe fire on the draft's wording - its noneOf stays. That list
        // is hand-curated and really does prevent the collision in production; dropping it would close
        // no hole and only reject drafts that are in fact disjoint.
        // The exclusions apply unchanged where the probe really is an utterance: the golden cases and
        // the skill phrases above.
        var openTrigger = WithoutExclusions(trigger);
        var ownSentences = CanonicalSentences(trigger);

        foreach (var recipe in existingRecipes)
        {
            var existingTrigger = Deserialize(recipe.TriggerJson);
            if (existingTrigger == null)
            {
                continue;
            }

            foreach (var sentence in CanonicalSentences(existingTrigger))
            {
                if (RecipeTriggerMatcher.Matches(openTrigger, sentence))
                {
                    return $"It would also fire on '{sentence}', which belongs to the recipe '{recipe.Name}'.";
                }
            }

            foreach (var sentence in ownSentences)
            {
                if (RecipeTriggerMatcher.Matches(existingTrigger, sentence))
                {
                    return $"Its own wording '{sentence}' already starts the recipe '{recipe.Name}'.";
                }
            }
        }

        return null;
    }

    private static RecipeTrigger WithoutExclusions(RecipeTrigger trigger) =>
        new() { AllOf = [.. trigger.AllOf], NoneOf = [] };

    // A recipe runs before function calling, so a trigger that a skill's own trigger phrase satisfies
    // steals that skill silently. This is the runtime counterpart of
    // SkillRecipeTriggerCrossQualityTests, which can only see the seeded recipes.
    private string? CheckSkillPhrases(RecipeTrigger trigger)
    {
        var checkedPhrases = 0;

        foreach (var descriptor in _skillRegistry.GetAllSkills())
        {
            if (descriptor.TriggerKeywords.Count == 0)
            {
                continue;
            }

            foreach (var phrase in Phrases(descriptor))
            {
                if (checkedPhrases++ >= MaxSkillPhrasesChecked)
                {
                    return null;
                }

                if (RecipeTriggerMatcher.Matches(trigger, phrase))
                {
                    return $"It would swallow '{phrase}', which is how users reach the skill "
                           + $"'{descriptor.Name}'.";
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> Phrases(SkillDescriptor descriptor)
    {
        foreach (var keyword in descriptor.TriggerKeywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            yield return keyword;
        }

        if (descriptor.TriggerKeywords.Count > 1)
        {
            yield return string.Join(' ', descriptor.TriggerKeywords);
        }
    }

    // Two kinds of probe sentence, and the second one is the one that matters.
    // The combinations of first-listed terms are what a user actually types: the shortest utterance that
    // satisfies every condition at once. But sampling two terms per condition misses a trigger whose
    // conditions each list several wordings - the German and the English stem of the same word sit in
    // one condition, and a rival trigger built from two of those wordings satisfies neither sampled
    // sentence while colliding with the recipe completely.
    // So the whole vocabulary of the trigger is probed as one sentence as well. It is not an utterance
    // anybody would type, and it does not have to be: a rival trigger matches it only if every one of
    // the rival's conditions is already covered by this trigger's own words, which is exactly the
    // collision being looked for and nothing else. Found by the integration fixture against the real
    // seed corpus, where a draft made of add-employee-to-group's own trigger words was let through.
    private static IReadOnlyList<string> CanonicalSentences(RecipeTrigger trigger)
    {
        var perCondition = trigger.AllOf
            .Select(condition => TermsOf([condition]).Take(2).ToList())
            .Where(terms => terms.Count > 0)
            .ToList();

        if (perCondition.Count == 0)
        {
            return [];
        }

        var sentences = new List<string> { string.Empty };

        foreach (var terms in perCondition)
        {
            var next = new List<string>();
            foreach (var prefix in sentences)
            {
                foreach (var term in terms)
                {
                    next.Add(string.IsNullOrEmpty(prefix) ? term : prefix + " " + term);
                    if (next.Count >= MaxCanonicalSentences)
                    {
                        break;
                    }
                }

                if (next.Count >= MaxCanonicalSentences)
                {
                    break;
                }
            }

            sentences = next;
        }

        var wholeVocabulary = string.Join(
            ' ', TermsOf(trigger.AllOf).Distinct(StringComparer.OrdinalIgnoreCase));

        if (wholeVocabulary.Length > 0)
        {
            sentences.Add(wholeVocabulary);
        }

        return sentences;
    }

    private static IEnumerable<string> TermsOf(IEnumerable<RecipeCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            foreach (var term in Concat(condition).Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                yield return term;
            }
        }
    }

    private static IEnumerable<string> Concat(RecipeCondition condition) =>
        (condition.AnyWordStart ?? [])
        .Concat(condition.AnySubstring ?? [])
        .Concat(condition.StartsWith ?? []);

    private static RecipeTrigger? Deserialize(string? triggerJson)
    {
        if (string.IsNullOrWhiteSpace(triggerJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RecipeTrigger>(triggerJson, TriggerJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
