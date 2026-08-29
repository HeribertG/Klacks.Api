// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One capability variant as the generator proposed it, before any oracle has looked at it. Kept as a
/// separate type from AgentRecipe so nothing half-judged can be written to the table by accident: a
/// draft becomes a recipe only where the learner decides it has earned it.
/// </summary>
/// <param name="Name">Slug proposed by the generator, still without the learned-namespace prefix</param>
/// <param name="GoalTranslations">Goal per core language, needed by the confirmation fallback</param>
using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record LearnedRecipeDraft(
    string Name,
    string Goal,
    IReadOnlyDictionary<string, string> GoalTranslations,
    RecipeTrigger Trigger,
    IReadOnlyList<RecipeStep> Steps);
