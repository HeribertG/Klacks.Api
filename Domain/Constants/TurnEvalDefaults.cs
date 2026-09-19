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
    /// How many measured items may error before the runner gives up, as long as not one measured item
    /// has succeeded yet. When they all errored and not one of them succeeded, the apparatus is broken
    /// (an unresolvable provider, a dead database) and every further item would only burn wall-clock
    /// time on the same failure. The runner aborts and persists nothing, because a run of nothing but
    /// infrastructure errors is not a measurement of the model. Excluded items count neither way, so a
    /// recipe-hijacked item among the first ones cannot disable the guard for the rest of the run.
    /// A goldset or an item cap smaller than this threshold can never reach it; such a run is caught
    /// by MaxErroredShareOfFullRun instead and persisted as partial.
    /// </summary>
    public const int InitialErrorAbortThreshold = 10;

    public const string InitialItemsAllErroredMessageFormat =
        "A turn eval run was aborted: {0} measured items errored without a single successful replay, which indicates a broken apparatus rather than a model weakness. Nothing was persisted. First error: {1}";

    /// <summary>
    /// The share of measured (non-excluded) items that may error before a run loses its full-run status.
    /// A run at or above this share - and likewise a run that measured nothing at all - is persisted with
    /// IsPartial = true.
    ///
    /// IsPartial filters the baseline and gate queries, NOT the history views: GetBestBaselineAsync,
    /// GetLatestFullRunAsync and ListRecentFullRunsAsync skip partial runs (as does the nightly script's
    /// "LATEST FULL RUN" figure), while GetLatestAsync, GetLatestPerModelAsync and GetHistoryAsync
    /// deliberately still return them, because a degraded run has to stay visible in the run history.
    /// Anything that RANKS or DECIDES on top of those three unfiltered methods has to exclude partial
    /// runs itself - see KlacksyModelCheckService.LoadEvalScoresAsync.
    /// </summary>
    public const double MaxErroredShareOfFullRun = 0.5;

    /// <summary>
    /// Fewest comparable completed runs the regression baseline needs. The baseline is the MEDIAN of their
    /// composites; below this count the run reports no regression at all, because a median of one or two
    /// noisy full runs (the spread between identical runs is about 0.04) cannot separate a real drop from noise.
    /// </summary>
    public const int MinBaselineRuns = 3;

    public const int ResponseTextMaxLength = 4000;

    public const string SyntheticLookupEntityId = "00000000-0000-4000-8000-00000000e7a1";

    public const string SyntheticLookupFallbackName = "match";

    public const string MemoryProbeEnvironmentVariable = "TURNEVAL_MEMORY_PROBE";

    public const string MemoryProbeEnabledValue = "1";

    public const int MemoryProbeEveryItems = 25;

    public const int LargeObjectHeapGenerationIndex = 3;

    public const string MemoryProbeLogPrefix = "TurnEval memory probe";

    public const string MemoryProbeLogTemplate = MemoryProbeLogPrefix +
        " item={Item} | before GC: residentMB={ResidentMb:F1}, managedHeapMB={ManagedMb:F1}, nativeMB={NativeMb:F1}, allocatedMB={AllocatedMb:F1}, privateMB={PrivateMb:F1}, threads={Threads}, handles={Handles}" +
        " | after full GC: retainedManagedMB={RetainedMb:F1}, heapMB={HeapMb:F1}, fragmentedMB={FragmentedMb:F1}, committedMB={CommittedMb:F1}, lohMB={LohMb:F1}, residentMB={ResidentAfterMb:F1}, nativeMB={NativeAfterMb:F1}, privateMB={PrivateAfterMb:F1}, threads={ThreadsAfter}, handles={HandlesAfter}" +
        " | collections gen0={Gen0}, gen1={Gen1}, gen2={Gen2}";
}
