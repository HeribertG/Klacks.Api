// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class RecipeEngineDefaults
{
    public const int PendingRecipeTtlMinutes = 30;

    public const string SlotReferencePrefix = "$";

    public const string CaptureSeparator = " as ";

    public const string AskStepInstructionTemplate =
        "RECIPE STEP — {0} Respond with that question in the user's language and do NOT call any tool.";

    public const string ConfirmationStepInstructionTemplate =
        "RECIPE CONFIRMATION — The user's message matched a guided flow by meaning, not by an explicit " +
        "trigger phrase: {0} Ask the user, in their own language, a short yes/no question confirming " +
        "whether they want you to start this flow. Do NOT call any tool and do NOT start the flow yet.";
}
