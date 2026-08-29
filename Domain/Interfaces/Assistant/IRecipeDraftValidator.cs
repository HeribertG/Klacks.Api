// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a generated capability may be written to agent_recipes. Pure and synchronous on
/// purpose: everything it checks must be decidable before the insert, because an enabled recipe forces
/// its steps on every instance from the moment it exists.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IRecipeDraftValidator
{
    /// <summary>
    /// Judges one draft against the recipes that already exist and the routing expectations already
    /// frozen.
    /// </summary>
    /// <param name="draft">The generated capability</param>
    /// <param name="existingRecipes">Every enabled recipe the trigger must stay disjoint from</param>
    /// <param name="goldenCases">Utterances whose routing must not change</param>
    RecipeDraftVerdict Validate(
        LearnedRecipeDraft draft,
        IReadOnlyList<AgentRecipe> existingRecipes,
        IReadOnlyList<SkillLearningGoldenCase> goldenCases);
}
