// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// IGoalSignalSource backed by the persisted proactive trigger history (agent_trigger_dispatches).
/// Aggregates dispatch rows from the last LookbackDays per (UserId, TriggerKind) into one GoalSignal
/// each, carrying how often the trigger fired and its first/last occurrence in that window.
/// </summary>
/// <param name="dispatchRepository">Read-only access to the trigger dispatch history.</param>
/// <param name="logger">Structured log of how many signals were collected per cycle.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Reflection;

public class TriggerHistoryGoalSignalSource : IGoalSignalSource
{
    private const int LookbackDays = 7;
    private const int MaxDispatchRows = 2000;

    // CuriosityQuestion is Klacksy's own generated output — feeding it back into reflection would
    // recreate the self-referential loop the design spec calls out as Risk 1. MuteSuggestion is
    // meta-noise about the trigger system itself, excluded here for the same reason
    // DismissStreakEvaluator excludes it from dismiss-streak evaluation.
    private static readonly HashSet<string> ExcludedTriggerKinds = new(StringComparer.Ordinal)
    {
        AgentTriggerKinds.CuriosityQuestion,
        AgentTriggerKinds.MuteSuggestion
    };

    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly ILogger<TriggerHistoryGoalSignalSource> _logger;

    public TriggerHistoryGoalSignalSource(
        IProactiveTriggerDispatchRepository dispatchRepository,
        ILogger<TriggerHistoryGoalSignalSource> logger)
    {
        _dispatchRepository = dispatchRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GoalSignal>> CollectAsync(CancellationToken cancellationToken = default)
    {
        var sinceUtc = DateTime.UtcNow.AddDays(-LookbackDays);
        var rows = await _dispatchRepository.GetSinceAsync(sinceUtc, MaxDispatchRows, cancellationToken);

        if (rows.Count == MaxDispatchRows)
        {
            _logger.LogWarning(
                "TriggerHistoryGoalSignalSource hit the {MaxDispatchRows}-row cap — the {LookbackDays}-day window " +
                "was truncated to the newest rows, so OccurrenceCount/FirstSeenUtc may be understated and some " +
                "users may be missing entirely from this cycle",
                MaxDispatchRows, LookbackDays);
        }

        var signals = rows
            .Where(r => r.CreateTime.HasValue
                        && !string.IsNullOrWhiteSpace(r.UserId)
                        && !ExcludedTriggerKinds.Contains(r.TriggerKind))
            .GroupBy(r => (r.UserId, r.TriggerKind))
            .Select(g =>
            {
                var timestamps = g.Select(r => r.CreateTime!.Value).ToList();
                var count = timestamps.Count;
                return new GoalSignal(
                    g.Key.UserId,
                    g.Key.TriggerKind,
                    BuildSummary(g.Key.TriggerKind, count),
                    count,
                    timestamps.Min(),
                    timestamps.Max());
            })
            .ToList();

        _logger.LogDebug(
            "TriggerHistoryGoalSignalSource collected {SignalCount} signal(s) from {RowCount} dispatch row(s) over the last {LookbackDays} day(s)",
            signals.Count, rows.Count, LookbackDays);

        return signals;
    }

    private static string BuildSummary(string triggerKind, int occurrenceCount) =>
        $"Trigger '{triggerKind}' fired {occurrenceCount} time(s) in the last {LookbackDays} days.";
}
