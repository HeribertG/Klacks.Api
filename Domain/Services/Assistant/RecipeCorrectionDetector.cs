// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Recognizes that the user is correcting the RECIPE while one of its ask steps is open, rather than
/// answering the question. Without it the whole message is raw-filled into the pending slot: the live
/// incident put "Nein du hast mich missverstanden, alle Mitarbeitern, Externen und Kunden. Plural nicht
/// singular" into a clientName slot and then searched 5000 clients for that sentence.
///
/// Deliberately precision-weighted. A false positive abandons a recipe the user already carried through
/// several turns, which costs more than one more turn of raw-filling - so this reports only the finding
/// it can defend and leaves weaker signals to the caller's default path.
///
/// The detector owns NO vocabulary of its own. That is the design, not an omission: keyword lists in
/// this area are the bug class that produced the incident it fixes, and they reach the four core
/// languages only, because noneOf is core-language vocabulary. Gate A delegates to
/// ImplicitCorrectionDetector, which already merges the core tokens with the `corrections` entries of
/// all 21 language packs. Gates C1/C2 read the RECIPE DEFINITION instead of the message, so they hold
/// in every language including the ones no list covers.
/// </summary>

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeCorrectionDetector
{
    /// <summary>
    /// Character floor separating a correction from a short entity name. It is NOT the precision gate
    /// and cannot be: the two calibration cases in the design spec are 50 and 49 characters long
    /// ("No, you misunderstood, all employees and customers" against "nicht dasselbe wie letztes Jahr,
    /// alle zwei Wochen"), so no threshold separates them, and picking 49/50 would be overfitting to
    /// two data points. The precision comes from gate C1 - the second of those is an answer to a
    /// criteria slot, and C1 only admits slots that feed a capturing entity search. This floor only
    /// has to be above any plausible person, client or group name.
    /// Characters, not words, on purpose: every word counter in this namespace tokenizes with \p{L}+,
    /// and Han/Kana are written without spaces, so an entire Chinese sentence is ONE token and any
    /// word-count floor rejects zh/ja structurally rather than judging it. A character floor is
    /// conservative for non-segmented scripts instead of blind to them - it is still calibrated on
    /// Latin text, so a CJK correction has to be longer than it semantically needs to be. Tightening
    /// that per script is known debt, not an oversight.
    /// </summary>
    internal const int MinCorrectionLengthInChars = 40;

    /// <summary>
    /// True when the message contradicts the recipe AND the open ask step cannot plausibly be what the
    /// user is answering. Caller order matters: RecipeCancellationDetector runs first, so "nein, doch
    /// nicht" stays a cancellation and never reaches this check.
    /// </summary>
    public static bool IsStrongCorrection(string? message, RecipeExecutionPlan? plan)
    {
        if (string.IsNullOrWhiteSpace(message) || plan == null)
        {
            return false;
        }

        // Gate A - contradiction. Delegated, and its own header says word matching alone is too broad to
        // use on its own; the state gates below are what make it usable here.
        if (!ImplicitCorrectionDetector.IsCorrectionSignal(message))
        {
            return false;
        }

        // Gate C1 - the pending slot expects an entity reference that a later search step must resolve
        // to exactly one row. A multi-clause sentence is not a plausible name for it.
        if (!plan.CurrentAskSlotFeedsACapturingSearch())
        {
            return false;
        }

        // Gate C2 - substance beyond the cue itself. This is what keeps "Montag, aber nicht Dienstag"
        // and the spec's "nicht dasselbe wie letztes Jahr, alle zwei Wochen" out: both carry a cue,
        // neither carries a correction.
        return message.Length >= MinCorrectionLengthInChars;
    }
}
