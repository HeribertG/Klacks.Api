// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Definition of the operator-authored recipe "dienst-aus-bestellung-schneiden" (turn one order into
/// a multi-part shift): the recipe name, its ordered step skills (resolve customer -> create one open order
/// -> seal it -> cut its plannable shift into parts), the trigger keyword sets that engage the deterministic
/// forcing spine, and the model-facing per-step guidance appended while a step is forced. Sealing and cutting
/// each need the user's confirmation, so the recipe spans three turns: the request (up to the held seal), the
/// confirmation of the seal (redeems it and forces the cut up to its hold) and the confirmation of the cut.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class RecipeConstants
{
    public const string CutFromOrderRecipeName = "dienst-aus-bestellung-schneiden";

    public const string FindCustomerSkill = "find_customer_candidates";

    public const string CreateShiftSkill = "create_shift";

    public const string SealShiftSkill = "seal_shift";

    public const string CutShiftSkill = "cut_shift";

    public const string ClientIdParam = "clientId";

    public const string ShiftIdParam = "shiftId";

    // Result data properties the plan reads between steps: create_shift names the created order ShiftId (and flags a
    // reused order that is already sealed with IsSealed - its ShiftId is then the plannable shift), seal_shift names the
    // sealed order OrderId and the plannable shift it created PlannableShiftId.
    public const string CreatedOrderIdDataProperty = "ShiftId";

    public const string CreatedOrderIsSealedDataProperty = "IsSealed";

    public const string SealedOrderIdDataProperty = "OrderId";

    public const string PlannableShiftIdDataProperty = "PlannableShiftId";

    // Create-intent verb stems, matched at a WORD BOUNDARY (\b<stem>) so mid-word false friends do not
    // trigger (e.g. "Pflege" no longer matches "lege"). The trailing vowel is kept where the bare form is a
    // noun ("plane"/"richte", not "plan"/"richt") so the noun alone does not signal a create intent.
    public static readonly IReadOnlyList<string> CreateIntentKeywords =
    [
        "erstell",
        "anleg",
        "leg",
        "mach",
        "richte",
        "einricht",
        "plane",
        "trag",
        "setz",
        "erfass",
        "brauch",
        "neue",
        "create",
    ];

    // Split-intent stems, matched at a word boundary. German participles with the "ge" infix are listed
    // explicitly ("aufgeteilt"/"dreigeteilt") because "\baufteil" / "\bteil" do not match them.
    public static readonly IReadOnlyList<string> SplitIntentKeywords =
    [
        "schneid",
        "teil",
        "aufteil",
        "aufgeteilt",
        "geteilt",
        "dreigeteilt",
        "zerteil",
        "drehplan",
        "split",
        "cut",
    ];

    // Split-intent multi-word phrases, matched as a case-insensitive substring.
    public static readonly IReadOnlyList<string> SplitIntentPhrases =
    [
        "dienste auf",
    ];

    // Topic anchors, matched as a case-insensitive SUBSTRING so German compounds still match
    // (Nachtdienst, Wochenenddienst, Pflegedienst). A broad anchor only raises precision (it is ANDed in).
    public static readonly IReadOnlyList<string> AnchorKeywords =
    [
        "dienst",
        "schicht",
        "bestell",
        "auftrag",
        "einsatz",
        "drehplan",
        "rund um die uhr",
    ];

    // Anchors matched at a word boundary so a duration like "24h" matches but a year like "2024" does not.
    public static readonly IReadOnlyList<string> AnchorWordBoundaryKeywords =
    [
        "24",
    ];

    // When a message STARTS with one of these it is a question (how-to) or an edit of an existing thing,
    // not a request to create one order and cut it; the recipe stays silent.
    public static readonly IReadOnlyList<string> RequestBlockingOpeners =
    [
        "wie ",
        "was ",
        "warum",
        "wieso",
        "weshalb",
        "wo ",
        "wer ",
        "welche",
        "welcher",
        "wieviel",
        "wie viele",
        "gibt es",
        "zeig",
        "erklär",
        "nenne",
        "liste",
        "aktualisier",
        "ändere",
        "ändre",
        "bearbeite",
        "update",
        "korrigier",
        "verschieb",
        "passe ",
        "wechsel",
    ];

    // Informational markers anywhere in the message also keep the recipe silent.
    public static readonly IReadOnlyList<string> InformationalMarkers =
    [
        "bedeutet",
        "unterschied",
        "wie funktioniert",
        "wo finde",
        "wie viele",
        "wozu dient",
    ];

    public const string FindCustomerStepNote =
        "RECIPE 'dienst-aus-bestellung-schneiden' — step 1 of 4: identify the customer the order is billed to. " +
        "Call find_customer_candidates with the customer name from the request as searchString. " +
        "When exactly one customer matches, the order proceeds automatically with that customer.";

    public const string CreateShiftStepNote =
        "RECIPE 'dienst-aus-bestellung-schneiden' — step 2 of 4: the customer is ALREADY chosen and its clientId is " +
        "supplied for you. Do NOT ask the user anything and do NOT navigate — call create_shift NOW to create EXACTLY " +
        "ONE shift (the order; it is created as an open order). Ignore any earlier instruction to ask which customer or " +
        "to wait for the user. Never call create_shift more than once. Pass groupIds only when the request names group " +
        "UUIDs; without them the seal places the order in the group derived from the customer's address. The next step " +
        "seals this order, the cut follows after that.";

    public const string SealShiftStepNote =
        "RECIPE 'dienst-aus-bestellung-schneiden' — step 3 of 4: now call seal_shift for the order you just created; its " +
        "shiftId is supplied for you. Sealing always needs the user's explicit confirmation: the server holds the call, " +
        "nothing is sealed yet, and its result carries a server-computed preview. Relay that preview to the user " +
        "unchanged, ask for the confirmation and stop. Tell the user that the cut into the requested parts follows once " +
        "the order is sealed. Do NOT call cut_shift now, do NOT navigate and do NOT call create_shift again.";

    public const string CutShiftStepNote =
        "RECIPE 'dienst-aus-bestellung-schneiden' — step 4 of 4: the order is sealed. Now call cut_shift to split its " +
        "plannable shift; its shiftId is supplied for you. Take the parts (and their names) from the user's request. " +
        "A cut always needs the user's explicit confirmation: the server holds the call, nothing is cut yet, and its " +
        "result carries a server-computed preview (parts with times and names, the default expenses every part carries). " +
        "Relay that preview to the user unchanged, ask for the confirmation and stop. " +
        "Do NOT navigate to the cut page and do NOT call create_shift or seal_shift again.";

    // {0} is the user's request that started the recipe in the turn before the seal confirmation: the parts and
    // their names are only in that request, the confirmation turn itself just says "yes".
    public const string CutShiftContinuationStepNoteTemplate =
        CutShiftStepNote + " The user's request that started this flow was: \"{0}\"";

    // Appended once to the tool results when the recipe stopped on its seal step, so the recipe never ends silently.
    public const string CutRecipeSealStoppedNote =
        "\n\n[RECIPE 'dienst-aus-bestellung-schneiden' PAUSED — the order exists, but seal_shift did not seal it (its " +
        "result above says why), so the cut into the requested parts did not run either. Tell the user why in one or two " +
        "sentences and that sealing and the cut are still open. If the order has no group, ask the user which group it " +
        "belongs to; once the user names it, find it with list_groups, link it to the order with add_shift_to_group and " +
        "call seal_shift again. Do NOT call cut_shift before the order is sealed.]";
}
