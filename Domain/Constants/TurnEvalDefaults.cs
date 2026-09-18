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
    /// an expected mutation, one more choice on a synthetic lookup result. Enforced as a post-condition
    /// in TurnReplayService, in every build configuration - a replay that recorded more steps has broken
    /// an invariant the cost estimate and the reached verdict both rest on, and must not report a result.
    /// The guard is fail-fast by design: TurnEvalRunnerService does not catch the resulting exception, so
    /// a violation aborts the whole run instead of persisting rows scored under a broken replay contract.
    /// </summary>
    public const int MaxReplaySteps = 2;

    public const string ReplayStepLimitExceededMessage =
        "A turn replay recorded more provider calls than the two-step replay limit allows.";

    /// <summary>
    /// How many leading items may fail before the runner gives up. When the first items in a row all
    /// errored and not one of them succeeded, the apparatus is broken (an unresolvable provider, a dead
    /// database) and every further item would only burn wall-clock time on the same failure. The runner
    /// aborts and persists nothing, because a run of nothing but infrastructure errors is not a
    /// measurement of the model.
    /// </summary>
    public const int InitialErrorAbortThreshold = 10;

    public const string InitialItemsAllErroredMessageFormat =
        "A turn eval run was aborted: the first {0} replayed items all failed without a single successful replay, which indicates a broken apparatus rather than a model weakness. Nothing was persisted. First error: {1}";

    /// <summary>
    /// The share of measured (non-excluded) items that may error before a run loses its full-run status.
    /// A run at or above this share is persisted with IsPartial = true, which removes it from every
    /// baseline and gate query (GetBestBaselineAsync, GetLatestFullRunAsync, ListRecentFullRunsAsync)
    /// and from the nightly script's "LATEST FULL RUN" figure.
    /// </summary>
    public const double MaxErroredShareOfFullRun = 0.5;

    public const int ResponseTextMaxLength = 4000;

    public const string SyntheticLookupEntityId = "00000000-0000-4000-8000-00000000e7a1";

    public const string SyntheticLookupFallbackName = "match";
}
