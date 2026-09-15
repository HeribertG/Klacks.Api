// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Reasons recorded on an aborted recipe_runs row. RecipeRunDefaults.AbortReasonMaxLength is the column
/// cap and RecipeAbortReasonsGuardTests enforces it against every constant here.
/// Only the reason introduced by the ask-step correction guard lives here so far. The five pre-existing
/// reasons are still inline literals in LLMService; migrating them is separate work and deliberately not
/// bundled into the change that introduces this class, so a goldset regression stays attributable to one
/// behaviour change instead of to a rename spread over eight call sites in two chat loops.
/// </summary>
public static class RecipeAbortReasons
{
    /// <summary>
    /// The user corrected the recipe itself while one of its ask steps was open, so the run was
    /// abandoned instead of raw-filling the correction into the pending slot.
    /// </summary>
    public const string CorrectedDuringAskStep = "corrected during ask step";
}
