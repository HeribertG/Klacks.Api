// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Reasons recorded on an aborted recipe_runs row. RecipeRunDefaults.AbortReasonMaxLength is the column
/// cap and RecipeAbortReasonsGuardTests enforces it against every constant here - and names the five
/// reasons that are still inline literals in LLMService, so they are outside that guard.
/// </summary>
public static class RecipeAbortReasons
{
    /// <summary>
    /// The user corrected the recipe itself while one of its ask steps was open, so the run was
    /// abandoned instead of raw-filling the correction into the pending slot.
    /// </summary>
    public const string CorrectedDuringAskStep = "corrected during ask step";
}
