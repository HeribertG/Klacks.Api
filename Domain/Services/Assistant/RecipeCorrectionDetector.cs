// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Recognizes that the user is correcting the RECIPE while one of its ask steps is open, rather than
/// answering the question. Without it the whole message is raw-filled into the pending slot: the live
/// incident put "Nein du hast mich missverstanden, alle Mitarbeitern, Externen und Kunden. Plural nicht
/// singular" into a clientName slot and then searched 5000 clients for that sentence.
///
/// Precision-weighted on purpose: a false positive abandons a recipe the user already carried through
/// several turns, which costs more than one more turn of raw-filling.
///
/// The detector owns no vocabulary. Gate A delegates to ImplicitCorrectionDetector, which already merges
/// the core tokens with the `corrections` entries of every language pack. Gate C1 reads the recipe
/// definition rather than the message, so it holds in languages no word list covers; gate C2 reads the
/// message and is only a floor, never the discriminator.
/// </summary>

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeCorrectionDetector
{
    /// <summary>
    /// Floor separating a correction from a short entity name. It cannot be the precision gate: the two
    /// calibration cases in the design spec are 50 and 49 characters long, so no threshold separates them
    /// and picking the value between them would be fitting to two data points. Precision comes from C1 —
    /// the 49-character case is an answer to a criteria slot, and C1 only admits slots that feed a
    /// capturing entity search. This floor only has to sit above any plausible person, client or group
    /// name.
    /// Characters rather than words because every word counter in this namespace tokenizes with \p{L}+,
    /// and Han/Kana are written without spaces: an entire Chinese sentence is one token, so a word-count
    /// floor rejects zh/ja outright instead of judging them. The floor is still calibrated on Latin text,
    /// which makes it too high for compact scripts — a faithful Chinese rendering of the incident this
    /// detector exists for is 39 characters and misses. Per-script thresholds are known debt.
    /// </summary>
    private const int MinCorrectionLengthInChars = 40;

    /// <summary>
    /// True when the message contradicts the recipe AND the open ask step cannot plausibly be what the
    /// user is answering.
    /// </summary>
    public static bool IsStrongCorrection(string? message, RecipeExecutionPlan? plan)
    {
        if (string.IsNullOrWhiteSpace(message) || plan == null)
        {
            return false;
        }

        // Gate A - contradiction. Its own header says word matching alone is too broad to use on its own;
        // C1 is what makes it usable here.
        if (!ImplicitCorrectionDetector.IsCorrectionSignal(message))
        {
            return false;
        }

        // The engine's own recovery state wins. An ambiguous capture rewinds the plan to this same ask
        // slot and asks the user to be more specific, and a disambiguation names two entities - "Nicht
        // die Maria Meier aus Bern, ich meine die Maria Meier aus Zürich" - which reads as a correction
        // by every gate below. Aborting there would discard the slots the user already supplied and burn
        // the one-shot rewind, turning the engine's two-attempt recovery into no attempts at all.
        if (plan.CaptureRewindUsed)
        {
            return false;
        }

        // Gate C1 - the pending slot expects an entity reference that a later search step must resolve to
        // exactly one row, so a multi-clause sentence is not a plausible value for it.
        if (!plan.CurrentAskSlotFeedsACapturingSearch())
        {
            return false;
        }

        // Gate C2 - substance beyond the cue itself. Keeps "Montag, aber nicht Dienstag" out: a cue and a
        // capturing slot, but no correction.
        return message.Length >= MinCorrectionLengthInChars;
    }
}
