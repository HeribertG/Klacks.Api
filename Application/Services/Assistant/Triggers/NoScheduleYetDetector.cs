// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Notices that an installation has never been planned and offers help, instead of leaving the
/// planner with reminders that presuppose a schedule which does not exist. Silent as soon as a
/// single work assignment exists anywhere — this is a setup observation, not a staffing metric, so
/// the moment planning starts the specialised detectors (unstaffed_shift, period_close_due,
/// next_period_scheduling_due) take over.
///
/// Emits at most ONE event per tick: the stage the installation is actually in. The stages are
/// ordered along the order -> shift -> assignment chain, so the message always names the next
/// missing step rather than listing everything that is absent.
///
/// The scan carries no cap — its result set is one row by construction — so it can promise a COMPLETE
/// fingerprint set and its ledger rows are resolved automatically. That matters here more than
/// elsewhere: the finding disappears the moment somebody schedules anything, and a stale open row
/// would keep claiming the installation is empty while a schedule is being built on top of it.
/// </summary>
/// <param name="activityProbe">Installation-wide setup snapshot.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class NoScheduleYetDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private readonly IScheduleActivityProbe _activityProbe;
    private readonly ILogger<NoScheduleYetDetector> _logger;

    public NoScheduleYetDetector(
        IScheduleActivityProbe activityProbe,
        ILogger<NoScheduleYetDetector> logger)
    {
        _activityProbe = activityProbe;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.NoScheduleYet;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var state = await _activityProbe.GetSetupStateAsync(cancellationToken);
        if (state.HasWork)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var stage = ScheduleSetupStages.For(state);

        _logger.LogInformation(
            "NoScheduleYet scan: no work assignment exists installation-wide, reporting setup stage {Stage}",
            stage);

        return [new NoScheduleYetTriggerEvent(stage)];
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var state = await _activityProbe.GetSetupStateAsync(cancellationToken);
        if (state.HasWork)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return new HashSet<string>(StringComparer.Ordinal)
        {
            AgentConditionLedgerPolicy.FingerprintFor(
                Kind,
                new NoScheduleYetTriggerEvent(ScheduleSetupStages.For(state)).DedupKey)
        };
    }
}
