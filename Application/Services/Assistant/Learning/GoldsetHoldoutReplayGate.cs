// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The gate for a description narrowed from goldset evidence. It replays only holdout items - the half
/// the optimizer never saw - and only those that involve the sharpened skill: items that expect it, and
/// items the skill currently wins although something else was expected. The second group is the point of
/// the narrowing; the first is what the narrowing may not cost.
/// An item that was already failing in the reference run cannot regress, exactly as in the golden-case
/// gate: the baseline is what the run measured, not perfection. A replay the provider never answered is
/// skipped for the same reason: a timeout is not evidence about a description, and blocked_regression is
/// a terminal verdict that would then carry a reason nothing observed.
/// Every replay is a paid provider call, so the set is capped and previously passing items come first -
/// they are the only ones that can turn red.
/// Replays run with administrator rights and an empty user identity for the same reason the routing
/// oracle does: a permission the triggering user happened to lack must not look like a regression.
/// </summary>
/// <param name="evalRunRepository">Finds the full run the reference verdicts come from</param>
/// <param name="evalRunItemRepository">The per-item verdicts of that run</param>
/// <param name="goldsetLoader">Supplies the message and expectation behind an item id</param>
/// <param name="replayService">Performs the single-turn replay against the live catalogue</param>
/// <param name="logger">One line per verdict</param>

using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class GoldsetHoldoutReplayGate : IGoldsetHoldoutReplayGate
{
    private const string NothingChosen = "nothing";

    private static readonly List<string> ProbeRights = [Roles.Admin];
    private static readonly string ProbeUserId = Guid.Empty.ToString();

    private readonly IEvalRunRepository _evalRunRepository;
    private readonly IEvalRunItemRepository _evalRunItemRepository;
    private readonly ITurnGoldsetLoader _goldsetLoader;
    private readonly ITurnReplayService _replayService;
    private readonly ILogger<GoldsetHoldoutReplayGate> _logger;

    public GoldsetHoldoutReplayGate(
        IEvalRunRepository evalRunRepository,
        IEvalRunItemRepository evalRunItemRepository,
        ITurnGoldsetLoader goldsetLoader,
        ITurnReplayService replayService,
        ILogger<GoldsetHoldoutReplayGate> logger)
    {
        _evalRunRepository = evalRunRepository;
        _evalRunItemRepository = evalRunItemRepository;
        _goldsetLoader = goldsetLoader;
        _replayService = replayService;
        _logger = logger;
    }

    public async Task<GoldsetHoldoutReplayVerdict> EvaluateAsync(
        string sharpenedSkillName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sharpenedSkillName))
        {
            return GoldsetHoldoutReplayVerdict.NotMeasured;
        }

        var run = await _evalRunRepository.GetLatestFullRunAsync(
            TurnEvalDefaults.DefaultGoldset, TurnEvalScorer.ScorerVersion, cancellationToken);

        if (run == null || string.IsNullOrWhiteSpace(run.Model))
        {
            _logger.LogInformation(
                "Targeted holdout replay skipped for {Skill}: no full run of '{Goldset}' at scorer version {Version}",
                sharpenedSkillName, TurnEvalDefaults.DefaultGoldset, TurnEvalScorer.ScorerVersion);
            return GoldsetHoldoutReplayVerdict.NotMeasured;
        }

        IReadOnlyList<TurnGoldsetItem> goldsetItems;
        try
        {
            goldsetItems = await _goldsetLoader.LoadAsync(
                TurnEvalDefaults.DefaultGoldset, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Targeted holdout replay skipped for {Skill}: goldset unreadable",
                sharpenedSkillName);
            return GoldsetHoldoutReplayVerdict.NotMeasured;
        }

        var itemsById = goldsetItems.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var rows = await _evalRunItemRepository.ListByRunAsync(run.Id, cancellationToken);

        var candidates = rows
            .Where(row => GoldsetPartitioner.IsHoldout(row.ItemId))
            .Where(row => Involves(row, sharpenedSkillName))
            .Where(row => itemsById.ContainsKey(row.ItemId))
            .OrderByDescending(row => row.SelectionHit == true)
            .ThenBy(row => row.ItemId, StringComparer.Ordinal)
            .Take(SkillLearningDefaults.MaxTargetedHoldoutReplaysPerProposal)
            .ToList();

        if (candidates.Count == 0)
        {
            _logger.LogInformation(
                "Targeted holdout replay skipped for {Skill}: no holdout item of the last full run involves it",
                sharpenedSkillName);
            return GoldsetHoldoutReplayVerdict.NotMeasured;
        }

        var regressions = new List<string>();
        var answered = 0;

        foreach (var row in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = itemsById[row.ItemId];
            var replay = await _replayService.ReplayAsync(
                item, run.Model!, ProbeUserId, ProbeRights, cancellationToken);

            if (replay is not { Success: true })
            {
                _logger.LogDebug(
                    "Targeted holdout replay of item {ItemId} was not answered: {Error}",
                    item.Id, replay?.Error);
                continue;
            }

            answered++;
            var scored = TurnEvalScorer.ScoreItem(item, replay);

            if (row.SelectionHit == true && scored.SelectionHit != true)
            {
                regressions.Add(
                    $"'{item.Id}' ({item.Message}) no longer selects '{item.ExpectedTool}' "
                        + $"but '{scored.ChosenTool ?? NothingChosen}'");
            }
        }

        if (answered == 0)
        {
            _logger.LogWarning(
                "Targeted holdout replay for {Skill}: none of the {Replayed} replay(s) were answered, so "
                    + "nothing was measured",
                sharpenedSkillName, candidates.Count);
            return GoldsetHoldoutReplayVerdict.NotMeasured;
        }

        _logger.LogInformation(
            "Targeted holdout replay for {Skill}: {Replayed} item(s) answered, {Regressions} regression(s)",
            sharpenedSkillName, answered, regressions.Count);

        return new GoldsetHoldoutReplayVerdict(true, regressions);
    }

    private static bool Involves(EvalRunItem row, string skillName) =>
        string.Equals(row.ExpectedTool, skillName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(row.ChosenTool, skillName, StringComparison.OrdinalIgnoreCase);
}
