// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates a turn-selection eval run: loads the goldset, replays every item
/// sequentially against the requested model, resolves name slots deterministically,
/// aggregates the scorecard and persists one EvalRun per model. The regression is measured
/// against the BEST completed run of the same goldset, model, item count and scorer version
/// - never against the latest run (which would let quality ratchet down by the tolerance on
/// every run) and never against a run over a different number of items.
///
/// Every replayed item is additionally persisted as an eval_run_items row, written in one batch after
/// the run itself because the rows carry a foreign key to it. Without those rows a run is a single
/// number and the two causes behind a miss cannot be told apart after the fact.
/// Items are replayed with the lookup follow-up, so a run reports both the strict first-choice verdict
/// and whether the expected tool was reached.
///
/// A run whose items all fail measures the apparatus, not the model: once
/// TurnEvalDefaults.InitialErrorAbortThreshold measured items have errored without a single success the
/// runner throws and persists nothing. A run that still ends at or above
/// TurnEvalDefaults.MaxErroredShareOfFullRun errored items, and a run that measured nothing at all, are
/// persisted with IsPartial = true so neither can become a baseline.
/// </summary>

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnEvalRunnerService : ITurnEvalRunnerService
{
    private readonly ITurnGoldsetLoader _goldsetLoader;
    private readonly ITurnReplayService _replayService;
    private readonly ISlotEntityResolver _slotEntityResolver;
    private readonly IEvalRunRepository _evalRunRepository;
    private readonly IEvalRunItemRepository _evalRunItemRepository;
    private readonly ILogger<TurnEvalRunnerService> _logger;

    public TurnEvalRunnerService(
        ITurnGoldsetLoader goldsetLoader,
        ITurnReplayService replayService,
        ISlotEntityResolver slotEntityResolver,
        IEvalRunRepository evalRunRepository,
        IEvalRunItemRepository evalRunItemRepository,
        ILogger<TurnEvalRunnerService> logger)
    {
        _goldsetLoader = goldsetLoader;
        _replayService = replayService;
        _slotEntityResolver = slotEntityResolver;
        _evalRunRepository = evalRunRepository;
        _evalRunItemRepository = evalRunItemRepository;
        _logger = logger;
    }

    public async Task<TurnEvalRunResult> RunAsync(
        string goldset,
        string modelId,
        int? maxItems,
        string userId,
        List<string> userRights,
        CancellationToken cancellationToken = default)
    {
        var allItems = await _goldsetLoader.LoadAsync(goldset, cancellationToken);
        return await RunAsync(goldset, allItems, modelId, maxItems, userId, userRights, cancellationToken);
    }

    public async Task<TurnEvalRunResult> RunAsync(
        string goldset,
        IReadOnlyList<TurnGoldsetItem> allItems,
        string modelId,
        int? maxItems,
        string userId,
        List<string> userRights,
        CancellationToken cancellationToken = default)
    {
        var items = maxItems.HasValue ? allItems.Take(maxItems.Value).ToList() : allItems.ToList();
        var isCapped = items.Count != allItems.Count;

        var runId = Guid.NewGuid();
        var runStopwatch = Stopwatch.StartNew();
        var itemResults = new List<TurnEvalItemResult>(items.Count);
        var itemRows = new List<EvalRunItem>(items.Count);
        string? providerId = null;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var replay = await _replayService.ReplayWithLookupFollowUpAsync(
                item, modelId, userId, userRights, cancellationToken);
            providerId ??= replay.ProviderId;

            var resolvedNameSlots = await ResolveNameSlotsAsync(item, replay, cancellationToken);
            var scored = TurnEvalScorer.ScoreItem(item, replay, resolvedNameSlots);
            itemResults.Add(scored);
            itemRows.Add(BuildItemRow(runId, item, replay, scored));
            AbortWhenTheApparatusIsDead(itemResults);
        }

        runStopwatch.Stop();

        var dimensions = TurnEvalScorer.Aggregate(itemResults);
        var composite = TurnEvalScorer.ComputeComposite(dimensions);

        // IsPartial means "not comparable to a completed run", for either of two reasons: the run was
        // capped and covers a different population, or it is no measurement of the model - too many
        // items errored, or nothing was measured at all. Neither may serve as, or be judged against,
        // a baseline.
        var isPartial = isCapped || IsDegradedMeasurement(itemResults);

        var baseline = isPartial
            ? null
            : await _evalRunRepository.GetBestBaselineAsync(
                goldset, modelId, dimensions.ItemsTotal, TurnEvalScorer.ScorerVersion, cancellationToken);
        decimal? regression = baseline == null ? null : (decimal)composite - baseline.CompositeScore;

        var evalRun = new EvalRun
        {
            Id = runId,
            Goldset = goldset,
            Provider = providerId,
            Model = modelId,
            CompositeScore = (decimal)composite,
            DimensionsJson = JsonSerializer.Serialize(dimensions),
            RegressionVsBaseline = regression,
            ItemsTotal = dimensions.ItemsTotal,
            ItemsPassed = dimensions.ItemsPassed,
            DurationMs = (int)runStopwatch.ElapsedMilliseconds,
            ScorerVersion = TurnEvalScorer.ScorerVersion,
            IsPartial = isPartial,
            CreateTime = DateTime.UtcNow
        };

        await _evalRunRepository.AddAsync(evalRun, cancellationToken);

        // After the run, never before: the item rows carry a foreign key to it.
        await _evalRunItemRepository.AddRangeAsync(itemRows, cancellationToken);

        _logger.LogInformation(
            "TurnEvalRun {Goldset} model {Model} scorerVersion={ScorerVersion} partial={Partial}: composite={Composite:F4}, tool={Tool:F2}, slot={Slot:F2}, noTool={NoTool:F2}, recipe={Recipe:F2}, honesty={Honesty:F2}, nameRes={NameRes:F2}, avgLatencyMs={AvgLatencyMs:F0}, items={Items}, excluded={Excluded}, errored={Errored}, cost={Cost:F4}, retrievalHit={RetrievalHit:F3}, selectionHit={SelectionHit:F3}, reachedHit={ReachedHit:F3}, lookupDetour={LookupDetour:F3}, regression={Regression}",
            goldset.ForLog(), modelId.ForLog(), TurnEvalScorer.ScorerVersion, isPartial, composite,
            dimensions.ToolAccuracy ?? -1, dimensions.SlotAccuracy ?? -1,
            dimensions.NoToolAccuracy ?? -1, dimensions.RecipeAccuracy ?? -1,
            dimensions.HonestyAccuracy ?? -1, dimensions.NameResolutionAccuracy ?? -1,
            dimensions.AvgLatencyMs,
            dimensions.ItemsTotal, dimensions.ItemsExcluded, dimensions.ItemsErrored,
            dimensions.TotalCost, dimensions.RetrievalHit ?? -1, dimensions.SelectionHit ?? -1,
            dimensions.ReachedHit ?? -1, dimensions.LookupDetourRate ?? -1, regression);

        return new TurnEvalRunResult
        {
            Run = evalRun,
            Dimensions = dimensions,
            Items = itemResults
        };
    }

    /// <summary>
    /// Aborts as soon as InitialErrorAbortThreshold measured items have errored without one of them
    /// having succeeded first. The verdict is recomputed over all results so far rather than tested at
    /// one fixed item count: a single excluded item among the leading ones used to shift every later
    /// item past that one checkpoint and disable the abort for the rest of the run. An excluded item
    /// counts as neither a success nor an error - the apparatus was never asked about it.
    /// </summary>
    private static void AbortWhenTheApparatusIsDead(IReadOnlyList<TurnEvalItemResult> itemResults)
    {
        var measured = itemResults.Where(i => !i.Excluded).ToList();
        if (measured.Any(i => !i.Errored)
            || measured.Count(i => i.Errored) < TurnEvalDefaults.InitialErrorAbortThreshold)
        {
            return;
        }

        throw new InvalidOperationException(string.Format(
            CultureInfo.InvariantCulture,
            TurnEvalDefaults.InitialItemsAllErroredMessageFormat,
            TurnEvalDefaults.InitialErrorAbortThreshold,
            measured.Select(i => i.Error).FirstOrDefault(e => !string.IsNullOrEmpty(e)) ?? string.Empty));
    }

    /// <summary>
    /// Whether the numbers describe the apparatus instead of the model, for either of two reasons.
    /// A run that measured nothing at all - every item excluded, or an empty goldset - is degraded by
    /// definition: its composite is the zero that ComputeComposite returns for an empty weight total,
    /// not a measured score, and persisting that as a full run would offer a fabricated figure as the
    /// latest full run of the goldset. Otherwise the errored share of the measured items decides.
    /// </summary>
    private static bool IsDegradedMeasurement(IReadOnlyList<TurnEvalItemResult> itemResults)
    {
        var measured = itemResults.Count(i => !i.Excluded);
        if (measured == 0)
        {
            return true;
        }

        var errored = itemResults.Count(i => !i.Excluded && i.Errored);
        return (double)errored / measured >= TurnEvalDefaults.MaxErroredShareOfFullRun;
    }

    private static EvalRunItem BuildItemRow(
        Guid runId, TurnGoldsetItem item, TurnReplayResult replay, TurnEvalItemResult scored) =>
        new()
        {
            Id = Guid.NewGuid(),
            EvalRunId = runId,
            ItemId = item.Id,
            Locale = item.Locale,
            ExpectedTool = item.ExpectedTool,
            ChosenTool = scored.ChosenTool,
            ToolsetNamesJson = JsonSerializer.Serialize(replay.AvailableToolNames),
            RetrievalHit = scored.RetrievalHit,
            SelectionHit = scored.SelectionHit,
            ReachedHit = scored.ReachedHit,
            ChosenArgsJson = replay.ToolParameters.Count == 0 ? null : JsonSerializer.Serialize(replay.ToolParameters),
            ResponseText = Truncate(replay.Content),
            ToolSequenceJson = JsonSerializer.Serialize(
                replay.Steps.Select(step => new { tool = step.Tool, parameters = step.Parameters })),
            Passed = scored.Passed,
            LatencyMs = (int)Math.Min(scored.LatencyMs, int.MaxValue),
            CreateTime = DateTime.UtcNow
        };

    private static string? Truncate(string? text) =>
        string.IsNullOrEmpty(text)
            ? null
            : text.Length <= TurnEvalDefaults.ResponseTextMaxLength
                ? text
                : text[..TurnEvalDefaults.ResponseTextMaxLength];

    private async Task<IReadOnlyDictionary<string, bool>?> ResolveNameSlotsAsync(
        TurnGoldsetItem item,
        TurnReplayResult replay,
        CancellationToken cancellationToken)
    {
        var nameSlots = item.ExpectedSlots
            .Where(s => s.Match == SlotMatchMode.ResolvedEntityId && s.Entity != null)
            .ToList();

        if (nameSlots.Count == 0 || !replay.Success || replay.ChosenTool == null)
        {
            return null;
        }

        var verdicts = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var slot in nameSlots)
        {
            verdicts[slot.Name] = await _slotEntityResolver.ResolvesToExpectedEntityAsync(
                slot.Entity!, replay.ToolParameters, cancellationToken);
        }

        return verdicts;
    }
}
