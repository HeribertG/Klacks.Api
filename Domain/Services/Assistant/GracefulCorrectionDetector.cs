// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a chat message corrects the assistant's immediately preceding turn ("Nein, ich
/// meinte …") rather than declining it, asking something new or making a fresh request. Six gates,
/// AND-ed in the order below, each precision-biased: a missed correction costs one turn, a false one
/// re-routes a request the user never made.
///
/// G0 anchor       - a previous action exists, made a tool call, is not superseded and is younger than
///                   GracefulCorrectionDefaults.CorrectionWindowMinutes.
/// G1 no recipe    - a paused or engaging recipe owns the correction path of its own (TP2). This is not
///                   implied by G0: a recipe whose first step calls a tool and then pauses on an ask
///                   leaves BOTH a previous action and a pending recipe, so the caller has to ask the
///                   pending-recipe store and pass the answer in.
/// G2 signal       - ImplicitCorrectionDetector, i.e. the 25-language negation vocabulary. The trigger,
///                   never the evidence: "nicht" occurs in plenty of ordinary German sentences.
/// G3 content      - a bare negation stays a decline; it answers a question rather than correcting a
///                   routing decision.
/// G4 no question  - a message that reads as an independent question stays a question. Checked twice:
///                   once on the raw message, once on the message with its leading negation lead
///                   stripped (DeclineDetector.StripNegationLead). A correction opens with "Nein, ..." in
///                   the same clause as what follows, with no sentence terminator between them, so
///                   RecipeTopicSwitchDetector's sentence splitter can never see the question after the
///                   comma as its own sentence unless the lead is removed first.
/// G5 does not route alone - the vocabulary-free gate and the load-bearing one: if the correction ALONE
///                   already guarantees a skill deterministically ("Bitte den Dienst nicht am Montag,
///                   sondern Dienstag eintragen"), it is a self-contained request and is served as one.
///
/// Evaluate returns the gate that rejected instead of a bool, so the caller can log which one did.
/// The production caller passes correctionRoutesAlone: false for gates G0-G4 and only runs the G5 probe
/// - then applies its verdict itself - once those cheaper gates already passed, so the (comparatively
/// expensive) probe is never paid for a message that was going to be rejected anyway.
/// </summary>
/// <param name="message">The raw user message that started this turn.</param>
/// <param name="lastAction">The previous-action record, or null when none was stored.</param>
/// <param name="recipeIsActive">Whether a recipe is paused or engaging on this turn (G1).</param>
/// <param name="correctionRoutesAlone">Whether the correction alone guarantees a skill (G5).</param>
/// <param name="nowUtc">Clock for the window check; UTC, because this is elapsed time, not a calendar day.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GracefulCorrectionDetector
{
    public static GracefulCorrectionGate Evaluate(
        string? message,
        AssistantLastAction? lastAction,
        bool recipeIsActive,
        bool correctionRoutesAlone,
        DateTime nowUtc)
    {
        if (lastAction == null || !lastAction.CanAnchorCorrection(nowUtc))
        {
            return GracefulCorrectionGate.Anchor;
        }

        if (recipeIsActive)
        {
            return GracefulCorrectionGate.ActiveRecipe;
        }

        if (!ImplicitCorrectionDetector.IsCorrectionSignal(message))
        {
            return GracefulCorrectionGate.Signal;
        }

        if (DeclineDetector.IsBareNegation(message))
        {
            return GracefulCorrectionGate.BareNegation;
        }

        if (RecipeTopicSwitchDetector.IsTopicSwitch(message)
            || RecipeTopicSwitchDetector.IsTopicSwitch(DeclineDetector.StripNegationLead(message)))
        {
            return GracefulCorrectionGate.TopicSwitch;
        }

        return correctionRoutesAlone ? GracefulCorrectionGate.RoutesAlone : GracefulCorrectionGate.Passed;
    }

    /// <summary>
    /// The skills this turn must not offer again: everything the previous turn called, minus
    /// confirm_pending_action, which is the user's only way to redeem a held action and must never be
    /// taken off the table. Always-on skills are exempt too, but that exemption belongs to the assembler,
    /// which is the only place that knows which skills are always-on.
    /// </summary>
    public static IReadOnlyList<string> ExcludedSkillNames(AssistantLastAction lastAction) =>
        lastAction.Calls
            .Select(c => c.SkillName)
            .Where(name => !string.IsNullOrWhiteSpace(name)
                && !string.Equals(name, AutonomyDefaults.ConfirmPendingActionSkillName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
