// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shadow-mode evaluator: when a recipe matches by keyword trigger, it computes a semantic "margin"
/// (best served-skill rerank score minus best foreign-skill rerank score) for the RAW user message and
/// logs it for later threshold calibration. It is a pure observer — it MUST NEVER influence the routing
/// decision (needsConfirmation), which stays exclusively with CompetingSkillIntentDetector.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IRecipeSkillMarginEvaluator
{
    /// <summary>
    /// Computes and logs the shadow margin for a keyword-matched recipe. Returns the computed result for
    /// testing/diagnostics, or null when shadow mode is disabled or no comparable candidates exist.
    /// </summary>
    /// <param name="request">The raw message, matched recipe, served skills and legacy verdict.</param>
    /// <param name="cancellationToken">Cancellation token for the turn.</param>
    Task<RecipeSkillMarginResult?> EvaluateAndLogAsync(
        RecipeSkillMarginRequest request,
        CancellationToken cancellationToken);
}
