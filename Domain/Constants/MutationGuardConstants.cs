// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants for the mutation-intent tool-call guard: the OpenAI tool_choice policy values,
/// the user-facing notice appended to a streamed answer when a state-changing request produced
/// no tool call, the model-facing nudge that forces a real tool call on the non-streaming retry,
/// the context note that resurfaces a pending confirmation token on the confirmation turn, the note that tells
/// the model a reply was ambiguous between languages and has to be asked again, the
/// user-facing prefix for a forced recipe step that failed on every attempt until the loop gave up,
/// and the neutral wording that replaces internal identifiers in that notice.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class MutationGuardConstants
{
    public const string ToolChoiceAuto = "auto";

    public const string ToolChoiceRequired = "required";

    public const string PendingConfirmationContextTemplate =
        "A previous action '{0}' is awaiting the user's confirmation and was NOT executed yet. The " +
        "user has just confirmed it. Call the 'confirm_pending_action' tool with 'confirmation_token' " +
        "set to '{1}' to execute it now.";

    public const string AmbiguousConfirmationContextTemplate =
        "The user's reply '{0}' is ambiguous: it means yes in some of the languages {1} and no in others, and " +
        "the interface language does not settle it. An action is awaiting the user's confirmation and was NOT " +
        "executed. Do not treat the reply as a confirmation or as a refusal and do not call " +
        "confirm_pending_action; ask the user to answer with an unambiguous yes or no in the language of the " +
        "interface.";

    public const string AmbiguousLanguageSeparator = ", ";

    public const string NoActionStreamNotice =
        "\n\nHinweis: Es wurde keine Aktion ausgeführt – bitte die Anfrage erneut stellen oder bestätigen.";

    public const string ForceToolNudge =
        "You described an action but called no tool, so nothing actually happened. You MUST now call the " +
        "appropriate tool to perform the requested change with the correct parameters. Do not answer in prose.";

    public const string RecipeStepFailedNoticePrefix =
        "\n\nHinweis: Die Aktion konnte nicht abgeschlossen werden. ";

    public const string RedactedInternalIdentifier = "die angeforderte Funktion";
}
