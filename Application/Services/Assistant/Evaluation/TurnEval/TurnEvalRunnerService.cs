// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates a turn-selection eval run: loads the goldset, replays every item
/// sequentially against the requested model, resolves name slots deterministically,
/// aggregates the scorecard and persists one EvalRun per model with regression against
/// the latest run of the same goldset and model.
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
        var items = maxItems.HasValue ? allItems.Take(maxItems.Value).ToList() : allItems.ToList();

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

        var baseline = await _evalRunRepository.GetLatestAsync(goldset, modelId, cancellationToken);
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
            CreateTime = DateTime.UtcNow
        };

        await _evalRunRepository.AddAsync(evalRun, cancellationToken);

        _logger.LogInformation(
            "TurnEvalRun {Goldset} model {Model}: composite={Composite:F4}, tool={Tool:F2}, slot={Slot:F2}, noTool={NoTool:F2}, nameRes={NameRes:F2}, items={Items}, excluded={Excluded}, errored={Errored}, cost={Cost:F4}, regression={Regression}",
            goldset.ForLog(), modelId.ForLog(), composite,
            dimensions.ToolAccuracy ?? -1, dimensions.SlotAccuracy ?? -1,
            dimensions.NoToolAccuracy ?? -1, dimensions.NameResolutionAccuracy ?? -1,
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
