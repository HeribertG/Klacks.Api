// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class RecipeEngineDefaults
{
    public const int PendingRecipeTtlMinutes = 30;

    public const string SlotReferencePrefix = "$";

    public const string CaptureSeparator = " as ";

    public const string AskStepInstructionTemplate =
        "RECIPE STEP — {0} Respond with that question in the user's language and do NOT call any tool.";
}
