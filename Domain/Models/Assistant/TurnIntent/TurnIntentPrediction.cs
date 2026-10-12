// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Classification of one chat turn by the turn-intent head.
/// </summary>
/// <param name="Label">Most probable class</param>
/// <param name="AskProbability">Calibrated probability of Ask</param>
/// <param name="DoProbability">Calibrated probability of Do</param>
/// <param name="OtherProbability">Calibrated probability of Other</param>
/// <param name="IsConfidentAsk">Ask wins and reaches the head's Ask threshold</param>
/// <param name="Locale">Labelset locale the request language resolved to, null when none matched</param>
/// <param name="LocaleEnabled">The trainer enabled the classifier for that locale (per-locale flip)</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant.TurnIntent;

public sealed record TurnIntentPrediction(
    TurnIntentLabel Label,
    double AskProbability,
    double DoProbability,
    double OtherProbability,
    bool IsConfidentAsk,
    string? Locale,
    bool LocaleEnabled)
{
    public bool MayActOnAsk => IsConfidentAsk && LocaleEnabled;
}
