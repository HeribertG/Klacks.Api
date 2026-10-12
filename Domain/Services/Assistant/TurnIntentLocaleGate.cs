// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The per-locale flip: the classifier may act for a locale only when its out-of-fold evidence shows an
/// improvement over MutationIntentDetector that survives sampling noise. Three conditions, all pre-registered in
/// TurnIntentDefaults: enough minimal pairs and confident-Ask predictions to judge; the 95 % Wilson lower bound of
/// the classifier's minimal-pair accuracy lies above the detector's minimal-pair accuracy; and the Wilson lower
/// bound of the confident-Ask precision (the decision the runtime acts on) reaches the precision floor. Also maps a
/// request language onto a labelset locale (exact tag first, then the primary subtag).
/// </summary>
/// <param name="verdict">Recorded out-of-fold counts of one locale</param>
/// <param name="language">Request language such as de, de-CH or zh-TW</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant.TurnIntent;

namespace Klacks.Api.Domain.Services.Assistant;

public static class TurnIntentLocaleGate
{
    private const char SubtagSeparator = '-';

    public static bool ShowsImprovement(TurnIntentLocaleVerdict verdict)
    {
        if (verdict.Pairs < TurnIntentDefaults.MinimumLocalePairs
            || verdict.ConfidentAskPredicted < TurnIntentDefaults.MinimumLocaleConfidentAskPredictions)
        {
            return false;
        }

        var classifierPairLowerBound = WilsonLowerBound(verdict.ClassifierPairsCorrect, verdict.Pairs);
        var detectorPairAccuracy = (double)verdict.DetectorPairsCorrect / verdict.Pairs;
        var askPrecisionLowerBound = WilsonLowerBound(verdict.ConfidentAskCorrect, verdict.ConfidentAskPredicted);

        return classifierPairLowerBound > detectorPairAccuracy
               && askPrecisionLowerBound >= TurnIntentDefaults.MinimumConfidentAskPrecisionLowerBound;
    }

    public static double WilsonLowerBound(int successes, int trials)
    {
        if (trials <= 0)
        {
            return 0.0;
        }

        var z = TurnIntentDefaults.WilsonZ;
        var p = (double)successes / trials;
        var zSquared = z * z;
        var centre = p + zSquared / (2.0 * trials);
        var margin = z * Math.Sqrt(p * (1.0 - p) / trials + zSquared / (4.0 * trials * trials));
        return (centre - margin) / (1.0 + zSquared / trials);
    }

    public static TurnIntentLocaleVerdict? ResolveLocale(string? language, IReadOnlyList<TurnIntentLocaleVerdict> locales)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var tag = language.Trim();
        var exact = locales.FirstOrDefault(l => string.Equals(l.Locale, tag, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact;
        }

        var separator = tag.IndexOf(SubtagSeparator);
        if (separator <= 0)
        {
            return null;
        }

        var primary = tag[..separator];
        return locales.FirstOrDefault(l => string.Equals(l.Locale, primary, StringComparison.OrdinalIgnoreCase));
    }
}
