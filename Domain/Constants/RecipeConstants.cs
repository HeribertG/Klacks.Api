// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Definition of the operator-authored recipe "dienst-aus-bestellung-schneiden" (turn one order into
/// a multi-part shift): the recipe name, its ordered step skills (resolve customer -> create one order
/// -> cut into parts), the trigger keyword sets that engage the deterministic forcing spine, and the
/// model-facing per-step guidance appended while a step is forced.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class RecipeConstants
{
    public const string CutFromOrderRecipeName = "dienst-aus-bestellung-schneiden";

    public const string FindCustomerSkill = "find_customer_candidates";

    public const string CreateShiftSkill = "create_shift";

    public const string CutShiftSkill = "cut_shift";

    public const string ClientIdParam = "clientId";

    public static readonly IReadOnlyList<string> CreateIntentKeywords =
    [
        "erstell",
        "anleg",
        "lege",
        "neue",
        "create",
    ];

    public static readonly IReadOnlyList<string> SplitIntentKeywords =
    [
        "schneid",
        "teile",
        "aufteil",
        "split",
        "cut",
        "drehplan",
        "geteilt",
        "dienste auf",
    ];

    public const string FindCustomerStepNote =
        "RECIPE 'dienst-aus-bestellung-schneiden' — step 1 of 3: identify the customer the order is billed to. " +
        "Call find_customer_candidates with the customer name from the request as searchString. " +
        "When exactly one customer matches, the order proceeds automatically with that customer.";

    public const string CreateShiftStepNote =
        "RECIPE 'dienst-aus-bestellung-schneiden' — step 2 of 3: the customer is ALREADY chosen and its clientId is " +
        "supplied for you. Do NOT ask the user anything and do NOT navigate — call create_shift NOW to create EXACTLY " +
        "ONE shift (the immutable order). Ignore any earlier instruction to ask which customer or to wait for the user. " +
        "Never call create_shift more than once. The next step will cut this single order into its parts.";

    public const string CutShiftStepNote =
        "RECIPE 'dienst-aus-bestellung-schneiden' — step 3 of 3: now call cut_shift to split the order you just " +
        "created. Use the ShiftId returned by the previous create_shift result as cut_shift's shiftId. " +
        "Do NOT navigate to the cut page and do NOT call create_shift again.";
}
