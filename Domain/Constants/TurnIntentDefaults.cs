// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Fixed settings of the turn-intent classifier shared by the runtime, the trainer and the guard tests. The
/// thresholds of the per-locale flip are pre-registered here: the trainer never tunes them on its own output.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class TurnIntentDefaults
{
    public const string ModeConfigKey = "Assistant:TurnIntentClassifier:Mode";
    public const string TimeoutConfigKey = "Assistant:TurnIntentClassifier:TimeoutMilliseconds";
    public const string VetoConfigKey = "Assistant:TurnIntentClassifier:VetoForcedToolCall";
    public const int DefaultTimeoutMilliseconds = 500;
    public const bool DefaultVetoForcedToolCall = false;
    public const int MinimumTimeoutMilliseconds = 50;
    public const int MaximumTimeoutMilliseconds = 5000;

    public const string HeadFileRelativePath = "Application/Klacksy/turn-intent-head.v1.json";
    public const int HeadFormatVersion = 1;

    public const string LogPrefix = "[turn-intent]";

    public const double AskConfidenceThreshold = 0.8;
    public const int MaxGuaranteedExplainSkills = 2;

    public const int MinimumLocalePairs = 20;
    public const int MinimumLocaleConfidentAskPredictions = 15;
    public const double MinimumConfidentAskPrecisionLowerBound = 0.85;
    public const double WilsonZ = 1.96;
}
