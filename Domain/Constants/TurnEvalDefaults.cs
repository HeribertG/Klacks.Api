// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fixed parameters of a turn-selection replay. The temperature is zero because an eval has to be a
/// measurement, not a sample: at 0.7 the daily 70-item run varied between 39 and 43 passes without any
/// code changing, which is larger than every effect the loop is meant to detect.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class TurnEvalDefaults
{
    public const double ReplayTemperature = 0.0;

    /// <summary>
    /// The goldset every part of the loop means when it says "the goldset": the seeder that imports it as
    /// golden cases, the learner that reads its selection misses and the gate that replays its holdout
    /// half all have to name the same file, or the partitions stop lining up.
    /// </summary>
    public const string DefaultGoldset = "turn-selection-v1";

    public const int ItemIdMaxLength = 128;

    public const int LocaleMaxLength = 8;

    public const int ToolNameMaxLength = 128;

    /// <summary>
    /// A replay asks the model at most twice: the first choice and, when that was a lookup in front of
    /// an expected mutation, one more choice on a synthetic lookup result.
    /// </summary>
    public const int MaxReplaySteps = 2;

    public const int ResponseTextMaxLength = 4000;

    public const string SyntheticLookupEntityId = "00000000-0000-4000-8000-00000000e7a1";

    public const string SyntheticLookupFallbackName = "match";
}
