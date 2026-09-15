// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

/// <summary>
/// Composes the message a re-resolve runs on after the user corrected a recipe, and caps what gets
/// persisted.
///
/// One source for both, deliberately. The toolset is assembled before LLMService runs, so the assembler
/// and the correction branch have to produce byte-identical text: two different compositions would
/// guarantee one recipe's step skills for the toolset while the plan resolves a different recipe, and the
/// forcing spine would then look for a skill that is not in the list and silently no-op. The same reason
/// applies to the memo in RecipeEngineService, whose key is the message text - a composition that differs
/// by a separator pays for two embedding rounds instead of reusing one.
/// </summary>
public static class RecipeCorrectionComposer
{
    /// <summary>
    /// The triggering message followed by the correction. Falls back to the correction alone when nothing
    /// was persisted - a pending row written before the column existed, or a caller that has no plan.
    /// A correction on its own usually resolves to nothing, because a message opening with a negation and
    /// carrying no mutation verb is not a standalone request; the fallback keeps that behaviour visible
    /// rather than inventing intent.
    /// </summary>
    public static string Compose(string? triggerMessage, string correction)
    {
        if (string.IsNullOrWhiteSpace(triggerMessage))
        {
            return correction;
        }

        return triggerMessage + RecipeEngineDefaults.CorrectionCompositeSeparator + correction;
    }

    /// <summary>
    /// Truncates for storage. A hard slice like RecipeRunRecorder.Truncate rather than an ellipsis: a
    /// half-stored triggering message still carries more intent than none, and trailing punctuation would
    /// only add characters that matching has to look past.
    /// </summary>
    public static string? CapForStorage(string? triggerMessage)
    {
        if (string.IsNullOrWhiteSpace(triggerMessage))
        {
            return null;
        }

        var trimmed = triggerMessage.Trim();
        return trimmed.Length <= RecipeEngineDefaults.PendingRecipeTriggerMessageMaxLength
            ? trimmed
            : trimmed[..RecipeEngineDefaults.PendingRecipeTriggerMessageMaxLength];
    }
}
