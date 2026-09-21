// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Reasons recorded on an aborted recipe_runs row. RecipeRunDefaults.AbortReasonMaxLength is the column
/// cap and RecipeAbortReasonsGuardTests enforces it against every constant here.
/// </summary>
public static class RecipeAbortReasons
{
    /// <summary>
    /// The user corrected the recipe itself while one of its ask steps was open, so the run was
    /// abandoned instead of raw-filling the correction into the pending slot.
    /// </summary>
    public const string CorrectedDuringAskStep = "corrected during ask step";

    /// <summary>
    /// The recipe was paused on its confirmation gate and the user's reply was not an affirmation, so the
    /// run was abandoned. Covers both a bare refusal and a reply that redirects the conversation
    /// elsewhere; which of the two it was is recorded on the trajectory's recipe outcome, not here.
    /// </summary>
    public const string ConfirmationDeclined = "confirmation declined";

    /// <summary>
    /// The user cancelled the recipe while one of its ask steps was open, so the run was abandoned.
    /// </summary>
    public const string CancelledDuringAskStep = "cancelled during ask step";

    /// <summary>
    /// The autonomy gate held one of the recipe's skills, which ends the forcing for the rest of the turn:
    /// the model has to ask the user, so the chain can no longer be driven step by step.
    /// </summary>
    public const string AutonomyGateHoldEndedRecipe = "autonomy gate hold ended the recipe";

    /// <summary>
    /// The cut recipe deactivated itself because the customer name matched more than one candidate, so the
    /// chain had nothing unambiguous to cut against.
    /// </summary>
    public const string AmbiguousCutRecipeMatch = "ambiguous customer match deactivated the cut recipe";

    /// <summary>
    /// The turn ended while the cut recipe was still mid-chain - the iteration budget ran out or the model
    /// stopped calling tools before the last step.
    /// </summary>
    public const string CutRecipeIncomplete = "turn ended before the cut recipe completed";
}
