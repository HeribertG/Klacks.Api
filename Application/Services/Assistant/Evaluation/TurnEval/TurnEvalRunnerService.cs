// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates a turn-selection eval run: loads the goldset, replays every item
/// sequentially against the requested model, resolves name slots deterministically,
/// aggregates the scorecard and persists one EvalRun per model. The regression is measured
/// against the BEST completed run of the same goldset, model, item count and scorer version
/// - never against the latest run (which would let quality ratchet down by the tolerance on
/// every run) and never against a run over a different number of items.
/// </summary>

using System.Diagnostics;
using System.Text.Json;
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
    private readonly ILogger<TurnEvalRunnerService> _logger;

    public TurnEvalRunnerService(
        ITurnGoldsetLoader goldsetLoader,
        ITurnReplayService replayService,
        ISlotEntityResolver slotEntityResolver,
        IEvalRunRepository evalRunRepository,
        ILogger<TurnEvalRunnerService> logger)
    {
        _goldsetLoader = goldsetLoader;
        _replayService = replayService;
        _slotEntityResolver = slotEntityResolver;
        _evalRunRepository = evalRunRepository;
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
        var isPartial = items.Count != allItems.Count;

        var runStopwatch = Stopwatch.StartNew();
        var itemResults = new List<TurnEvalItemResult>(items.Count);
        string? providerId = null;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var replay = await _replayService.ReplayAsync(item, modelId, userId, userRights, cancellationToken);
            providerId ??= replay.ProviderId;

            var resolvedNameSlots = await ResolveNameSlotsAsync(item, replay, cancellationToken);
            itemResults.Add(TurnEvalScorer.ScoreItem(item, replay, resolvedNameSlots));
        }

        runStopwatch.Stop();

        var dimensions = TurnEvalScorer.Aggregate(itemResults);
        var composite = TurnEvalScorer.ComputeComposite(dimensions);

        // A partial run covers a different population than any completed run, so it has no
        // comparable baseline at all and must not report a regression against one.
        var baseline = isPartial
            ? null
            : await _evalRunRepository.GetBestBaselineAsync(
                goldset, modelId, dimensions.ItemsTotal, TurnEvalScorer.ScorerVersion, cancellationToken);
        decimal? regression = baseline == null ? null : (decimal)composite - baseline.CompositeScore;

        var evalRun = new EvalRun
        {
            Id = Guid.NewGuid(),
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

        _logger.LogInformation(
            "TurnEvalRun {Goldset} model {Model} scorerVersion={ScorerVersion} partial={Partial}: composite={Composite:F4}, tool={Tool:F2}, slot={Slot:F2}, noTool={NoTool:F2}, recipe={Recipe:F2}, honesty={Honesty:F2}, nameRes={NameRes:F2}, avgLatencyMs={AvgLatencyMs:F0}, items={Items}, excluded={Excluded}, errored={Errored}, cost={Cost:F4}, regression={Regression}",
            goldset.ForLog(), modelId.ForLog(), TurnEvalScorer.ScorerVersion, isPartial, composite,
            dimensions.ToolAccuracy ?? -1, dimensions.SlotAccuracy ?? -1,
            dimensions.NoToolAccuracy ?? -1, dimensions.RecipeAccuracy ?? -1,
            dimensions.HonestyAccuracy ?? -1, dimensions.NameResolutionAccuracy ?? -1,
            dimensions.AvgLatencyMs,
            dimensions.ItemsTotal, dimensions.ItemsExcluded, dimensions.ItemsErrored,
            dimensions.TotalCost, regression);

        return new TurnEvalRunResult
        {
            Run = evalRun,
            Dimensions = dimensions,
            Items = itemResults
        };
    }

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
