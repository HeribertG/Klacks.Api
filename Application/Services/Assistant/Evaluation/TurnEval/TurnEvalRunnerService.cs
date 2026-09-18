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
/// A run whose items all fail at the start measures the apparatus, not the model: after
/// TurnEvalDefaults.InitialErrorAbortThreshold leading errors without a single success the runner throws
/// and persists nothing, and a run that still ends at or above TurnEvalDefaults.MaxErroredShareOfFullRun
/// errored items is persisted with IsPartial = true so it can never become a baseline.
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
        // capped and covers a different population, or so many items errored that the numbers measure
        // the apparatus rather than the model. Neither may serve as, or be judged against, a baseline.
        var isPartial = isCapped || IsErrorDegraded(itemResults);

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

    private static void AbortWhenTheApparatusIsDead(IReadOnlyList<TurnEvalItemResult> itemResults)
    {
        if (itemResults.Count != TurnEvalDefaults.InitialErrorAbortThreshold
            || !itemResults.All(i => i.Errored))
        {
            return;
        }

        throw new InvalidOperationException(string.Format(
            CultureInfo.InvariantCulture,
            TurnEvalDefaults.InitialItemsAllErroredMessageFormat,
            TurnEvalDefaults.InitialErrorAbortThreshold,
            itemResults.Select(i => i.Error).FirstOrDefault(e => !string.IsNullOrEmpty(e)) ?? string.Empty));
    }

    private static bool IsErrorDegraded(IReadOnlyList<TurnEvalItemResult> itemResults)
    {
        var measured = itemResults.Count(i => !i.Excluded);
        if (measured == 0)
        {
            return false;
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
