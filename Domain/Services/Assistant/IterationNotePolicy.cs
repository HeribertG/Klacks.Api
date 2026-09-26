// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Picks the single per-iteration instruction note appended to the volatile system prompt. The three
/// candidates are alternatives, not additions, and their precedence is what both chat loops relied on
/// inline: a forced confirmation wins over a forced recipe step, which wins over the completion note of a
/// read-only recipe (its final step's reply instructions, carried into the reply call), which wins over the
/// plan nudge, and the nudge only applies while the turn has not called a tool yet. Extracted so the precedence lives in one
/// place instead of in two hand-copied conditional expressions.
/// </summary>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class IterationNotePolicy
{
    /// <summary>
    /// Resolves the note for the iteration about to be sent to the provider.
    /// </summary>
    /// <param name="confirmThisIteration">True while the pending-confirmation gate narrows the turn.</param>
    /// <param name="pendingNote">The confirmation-gate note, used only when the gate applies.</param>
    /// <param name="forceRecipe">True while a recipe step is being forced this iteration.</param>
    /// <param name="recipeNote">The forced step's note, used only when a step is being forced.</param>
    /// <param name="suggestPlan">True when the plan-trigger heuristic considers the turn a plan candidate.</param>
    /// <param name="functionCallCount">Function calls the turn has made so far; the nudge stops after the first.</param>
    /// <param name="recipeCompletionNote">The completed read-only recipe's reply note, null when none completed this turn.</param>
    internal static string? Select(
        bool confirmThisIteration,
        string? pendingNote,
        bool forceRecipe,
        string? recipeNote,
        bool suggestPlan,
        int functionCallCount,
        string? recipeCompletionNote = null) =>
        confirmThisIteration ? pendingNote
            : forceRecipe ? recipeNote
            : recipeCompletionNote != null ? recipeCompletionNote
            : suggestPlan && functionCallCount == 0 ? PlanSkillDefaults.PlanNudgeNote
            : null;
}
