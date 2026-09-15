// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class RecipeEngineDefaults
{
    public const int PendingRecipeTtlMinutes = 30;

    public const string SlotReferencePrefix = "$";

    public const string CaptureSeparator = " as ";

    public const string CaptureArrayMarker = "[].";

    public const string AskStepInstructionTemplate =
        "RECIPE STEP — {0} Respond with that question in the user's language and do NOT call any tool.";

    public const string ConfirmationStepInstructionTemplate =
        "RECIPE CONFIRMATION — The user's message matched a guided flow by meaning, not by an explicit " +
        "trigger phrase: {0} Ask the user, in their own language, a short yes/no question confirming " +
        "whether they want you to start this flow. Do NOT call any tool and do NOT start the flow yet.";

    public const string GateHoldEndsRecipeNote =
        "\n\n[RECIPE ENDED — a step needed the user's explicit confirmation, so the guided flow was " +
        "stopped. Only the single confirmed action will run. Tell the user which remaining steps of " +
        "the flow were not carried out, so they can restart it if they still want them.]";

    public const string ConfirmationStepWithAlternativeInstructionTemplate =
        "RECIPE CONFIRMATION — The user's message matched a guided flow by meaning, not by an explicit " +
        "trigger phrase: {0} A second guided flow matched almost as closely: {1} Ask the user, in their " +
        "own language, a brief question asking which of the two flows they meant (or neither). Do NOT " +
        "call any tool and do NOT start any flow yet.";

    /// <summary>
    /// Separates a normal turn's own answer from the recipe's re-asked question when
    /// RecipeTopicSwitchDetector recognized the user's reply as an independent question: the turn answers
    /// it with the full toolset, then this joins the still-open ask question onto the same response.
    /// </summary>
    public const string TopicSwitchReaskSeparator = "\n\n";

    /// <summary>
    /// Cap for the persisted triggering message. Enforced in code, not only in the column configuration:
    /// EF InMemory ignores HasMaxLength, and this value is copied on every ask-pause rather than once.
    /// </summary>
    public const int PendingRecipeTriggerMessageMaxLength = 2000;

    /// <summary>
    /// Joins the triggering message and the correction into the composite that a re-resolve runs on. One
    /// newline rather than two: the result is fed to matching and embedding, not shown to a user, and a
    /// blank line invites a tokenizer to treat the halves as unrelated documents.
    /// </summary>
    public const string CorrectionCompositeSeparator = "\n";
}
