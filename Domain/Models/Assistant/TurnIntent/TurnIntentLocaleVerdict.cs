// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Out-of-fold evidence for one locale, recorded by the trainer next to the head weights. The flip decision is
/// recomputable from these counts alone (TurnIntentLocaleGate), which is what the guard test does.
/// </summary>
/// <param name="Locale">Labelset locale, e.g. de or zh-TW</param>
/// <param name="Enabled">Trainer verdict: the classifier may act for this locale</param>
/// <param name="Items">Non-ambiguous items of the locale</param>
/// <param name="Pairs">Ask/Do minimal pairs of the locale</param>
/// <param name="ClassifierPairsCorrect">Pairs with both members classified correctly out of fold</param>
/// <param name="DetectorPairsCorrect">Pairs with both members decided correctly by MutationIntentDetector</param>
/// <param name="ConfidentAskPredicted">Items the classifier called Ask at or above the confidence threshold</param>
/// <param name="ConfidentAskCorrect">Of those, items labelled Ask</param>
/// <param name="AskItems">Items labelled Ask</param>
/// <param name="DetectorAskPredicted">Items the detector mapping calls Ask (question lead, no mutation)</param>
/// <param name="DetectorAskCorrect">Of those, items labelled Ask</param>
/// <param name="DetectorMutationItems">Items the detector flags IsMutationIntent (would force tool_choice)</param>
/// <param name="VetoShadowPredicted">Detector-mutation items the classifier calls confident Ask (would veto)</param>
/// <param name="VetoShadowCorrect">Of those, items labelled Ask (a correct veto)</param>

namespace Klacks.Api.Domain.Models.Assistant.TurnIntent;

public sealed record TurnIntentLocaleVerdict(
    string Locale,
    bool Enabled,
    int Items,
    int Pairs,
    int ClassifierPairsCorrect,
    int DetectorPairsCorrect,
    int ConfidentAskPredicted,
    int ConfidentAskCorrect,
    int AskItems,
    int DetectorAskPredicted,
    int DetectorAskCorrect,
    int DetectorMutationItems,
    int VetoShadowPredicted,
    int VetoShadowCorrect);
