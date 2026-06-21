// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the mutation-intent tool-call guard: the OpenAI tool_choice policy values,
/// the user-facing notice appended to a streamed answer when a state-changing request produced
/// no tool call, the model-facing nudge that forces a real tool call on the non-streaming retry,
/// and the context note that resurfaces a pending confirmation token on the confirmation turn.
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

    public const string NoActionStreamNotice =
        "\n\nHinweis: Es wurde keine Aktion ausgeführt – bitte die Anfrage erneut stellen oder bestätigen.";

    public const string ForceToolNudge =
        "You described an action but called no tool, so nothing actually happened. You MUST now call the " +
        "appropriate tool to perform the requested change with the correct parameters. Do not answer in prose.";
}
