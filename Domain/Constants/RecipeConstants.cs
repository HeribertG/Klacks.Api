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
