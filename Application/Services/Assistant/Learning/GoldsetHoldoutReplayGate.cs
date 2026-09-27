// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Plans and runs the paired goldset replay of the description gate. The holdout items come from the latest
/// full run of the default goldset - the half the optimizer never saw - and only those that involve the skill:
/// items that expect it, and items it won although something else was expected. The train items are the misses
/// the proposal was built from, in the default goldset or its paraphrases. Previously passing holdout items
/// come first because only they can regress; recipe items are left out because a recipe decides them, not a
/// description. Every replay is a paid provider call, so both lists are capped.
/// A replay is scored on the tool actually chosen, not on SelectionHit: a narrowed description can drop the
/// expected tool from the offered list, and that is a miss, not an unmeasured item. An unanswered or recipe-
/// excluded replay is unmeasured - a timeout is not evidence about a description - and so is a replay that
/// throws, including an HTTP timeout that surfaces as a cancellation nobody requested: it costs its own item,
/// not the answers of the others. Only a cancellation of the run itself ends the replay.
/// Replays run with administrator rights and an empty user identity, exactly as the routing oracle does.
/// </summary>
/// <param name="evalRunRepository">Finds the full run the holdout items are chosen from</param>
/// <param name="evalRunItemRepository">The per-item verdicts of that run</param>
/// <param name="goldsetLoader">Supplies message and expectation behind an item id</param>
/// <param name="replayService">Performs the single-turn replay against the live catalogue</param>
/// <param name="learningOptionsProvider">Resolves the configured reference model the full run must match</param>
/// <param name="logger">One line per plan and per failed replay</param>

using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using SettingsKeys = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class GoldsetHoldoutReplayGate : IGoldsetHoldoutReplayGate
{
    private static readonly List<string> ProbeRights = [Roles.Admin];
    private static readonly string ProbeUserId = Guid.Empty.ToString();

    private readonly IEvalRunRepository _evalRunRepository;
    private readonly IEvalRunItemRepository _evalRunItemRepository;
    private readonly ITurnGoldsetLoader _goldsetLoader;
    private readonly ITurnReplayService _replayService;
    private readonly ISkillLearningOptionsProvider _learningOptionsProvider;
    private readonly ILogger<GoldsetHoldoutReplayGate> _logger;
    private readonly Dictionary<string, IReadOnlyDictionary<string, TurnGoldsetItem>?> _goldsets = new(StringComparer.Ordinal);

    public GoldsetHoldoutReplayGate(
        IEvalRunRepository evalRunRepository,
        IEvalRunItemRepository evalRunItemRepository,
        ITurnGoldsetLoader goldsetLoader,
        ITurnReplayService replayService,
        ISkillLearningOptionsProvider learningOptionsProvider,
        ILogger<GoldsetHoldoutReplayGate> logger)
    {
        _evalRunRepository = evalRunRepository;
        _evalRunItemRepository = evalRunItemRepository;
        _goldsetLoader = goldsetLoader;
        _replayService = replayService;
        _learningOptionsProvider = learningOptionsProvider;
        _logger = logger;
    }

    public async Task<GoldsetReplayPlan?> PlanAsync(
        string skillName, IReadOnlyList<GoldsetItemRef> trainMisses, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return null;
        }

        var learningOptions = await _learningOptionsProvider.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(learningOptions.ReferenceModel))
        {
            _logger.LogWarning(
                "Goldset gate not measurable for {Skill}: no learning reference model configured "
                    + "(neither {SettingKey} nor a default model in llm_models)",
                skillName, SettingsKeys.KLACKSY_LEARNING_REFERENCE_MODEL);
            return null;
        }

        var run = await _evalRunRepository.GetLatestFullRunAsync(
            TurnEvalDefaults.DefaultGoldset, TurnEvalScorer.ScorerVersion, learningOptions.ReferenceModel, cancellationToken);

        if (run == null || string.IsNullOrWhiteSpace(run.Model))
        {
            _logger.LogWarning(
                "Goldset gate not measurable for {Skill}: no full run of '{Goldset}' at scorer version {Version} "
                    + "for reference model '{ReferenceModel}'",
                skillName, TurnEvalDefaults.DefaultGoldset, TurnEvalScorer.ScorerVersion, learningOptions.ReferenceModel);
            return null;
        }

        var baseItems = await LoadAsync(TurnEvalDefaults.DefaultGoldset, cancellationToken);
        if (baseItems == null)
        {
            return null;
        }

        var rows = await _evalRunItemRepository.ListByRunAsync(run.Id, cancellationToken);
        var holdout = rows
            .Where(row => GoldsetPartitioner.IsHoldout(row.ItemId))
            .Where(row => Involves(row, skillName))
            .Where(row => baseItems.TryGetValue(row.ItemId, out var item) && item.ExpectedRecipe == null)
            .OrderByDescending(row => row.SelectionHit == true)
            .ThenBy(row => row.ItemId, StringComparer.Ordinal)
            .Take(SkillLearningDefaults.MaxTargetedHoldoutReplaysPerProposal)
            .Select(row => new GoldsetItemRef(TurnEvalDefaults.DefaultGoldset, row.ItemId))
            .ToList();

        var train = await ResolveTrainItemsAsync(trainMisses, cancellationToken);

        if (holdout.Count == 0 && train.Count == 0)
        {
            _logger.LogInformation(
                "Goldset gate has nothing to replay for {Skill}: no holdout item involves it and no train miss resolves",
                skillName);
        }

        return new GoldsetReplayPlan(run.Id, run.Model!, TurnEvalScorer.ScorerVersion, holdout, train);
    }

    public async Task<IReadOnlyDictionary<GoldsetItemRef, bool?>> ReplayAsync(
        GoldsetReplayPlan plan, CancellationToken cancellationToken = default)
    {
        var verdicts = new Dictionary<GoldsetItemRef, bool?>();

        foreach (var reference in plan.HoldoutItems.Concat(plan.TrainItems))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var items = await LoadAsync(reference.Goldset, cancellationToken);
            if (items == null || !items.TryGetValue(reference.ItemId, out var item))
            {
                verdicts[reference] = null;
                continue;
            }

            TurnReplayResult replay;
            try
            {
                replay = await _replayService.ReplayAsync(
                    item, plan.Model, ProbeUserId, ProbeRights, cancellationToken);
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "Gate replay of item {ItemId} of '{Goldset}' failed; the item counts as unmeasured",
                    reference.ItemId, reference.Goldset);
                verdicts[reference] = null;
                continue;
            }

            verdicts[reference] = Score(item, replay);
        }

        return verdicts;
    }

    private async Task<IReadOnlyList<GoldsetItemRef>> ResolveTrainItemsAsync(
        IReadOnlyList<GoldsetItemRef> trainMisses, CancellationToken cancellationToken)
    {
        var train = new List<GoldsetItemRef>();

        foreach (var reference in trainMisses.Distinct())
        {
            if (train.Count >= SkillLearningDefaults.MaxTargetedTrainReplaysPerProposal)
            {
                break;
            }

            if (!TurnEvalDefaults.LearningGoldsets.Contains(reference.Goldset, StringComparer.Ordinal)
                || !GoldsetPartitioner.IsTrain(reference.ItemId))
            {
                continue;
            }

            var items = await LoadAsync(reference.Goldset, cancellationToken);
            if (items != null && items.ContainsKey(reference.ItemId))
            {
                train.Add(reference);
            }
        }

        return train;
    }

    private async Task<IReadOnlyDictionary<string, TurnGoldsetItem>?> LoadAsync(
        string goldset, CancellationToken cancellationToken)
    {
        if (_goldsets.TryGetValue(goldset, out var cached))
        {
            return cached;
        }

        IReadOnlyDictionary<string, TurnGoldsetItem>? loaded;
        try
        {
            var items = await _goldsetLoader.LoadAsync(goldset, cancellationToken);
            loaded = items
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Goldset '{Goldset}' could not be read for the gate", goldset);
            loaded = null;
        }

        _goldsets[goldset] = loaded;
        return loaded;
    }

    private static bool? Score(TurnGoldsetItem item, TurnReplayResult? replay)
    {
        if (replay is not { Success: true })
        {
            return null;
        }

        var scored = TurnEvalScorer.ScoreItem(item, replay);
        if (scored.Excluded)
        {
            return null;
        }

        return item.ExpectedTool == null ? scored.NoToolCorrect == true : scored.ToolHit == true;
    }

    private static bool Involves(EvalRunItem row, string skillName) =>
        string.Equals(row.ExpectedTool, skillName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(row.ChosenTool, skillName, StringComparison.OrdinalIgnoreCase);
}
