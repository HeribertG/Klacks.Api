// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Notices that an installation which already organises itself in groups still carries a number of
/// staffable duties that belong to no group, and makes one quiet recommendation about it. Two
/// conditions must hold together:
///
/// (a) at least one group exists — read from the installation-wide setup snapshot, whose group probe is
/// a FLAT existence check. This is the exact inverse of ungrouped_workforce's own gate, so the two can
/// never speak in the same tick: an installation with no group at all needs its first group, not a
/// review of individual duties, and hearing both at once would put two different "start here" messages
/// in front of the same administrator;
///
/// (b) at least MinUngroupedShiftsForRecommendation such duties exist. A handful is ordinary — a duty
/// is created before anybody decides which group owns it — and a recommendation about it would be
/// noise.
///
/// Why it is worth saying at all: without a GroupItem a duty's assignments are never sealed by the
/// group-scoped period close and never reach a per-group payroll export, so the omission surfaces at
/// the end of a period rather than when the duty is created.
///
/// Emits at most ONE event per tick — the condition is a single installation-wide fact, not a row per
/// duty — and the fingerprint scan runs the IDENTICAL gate through the same private helper rather than
/// re-stating it. That is what makes the fingerprint set COMPLETE in both directions: the ledger row
/// resolves itself the moment the duties are assigned, and equally when the last group disappears.
/// Re-stating the gate in two places is the failure mode here, because a fingerprint scan narrower than
/// the emission gate resolves rows the same tick opened them.
/// </summary>
/// <param name="activityProbe">Installation-wide setup snapshot and the ungrouped-duty count.</param>
/// <param name="companyClock">Resolves "today" as the company's own local day for the validity window.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class UngroupedShiftsDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    /// <summary>
    /// The number of ungrouped duties from which the omission stops looking like work in progress.
    /// Class-local like UngroupedWorkforceDetector.MinEmployeesForGroupingSuggestion rather than a
    /// configuration setting: it is a judgement about when advice is worth giving, and an installation
    /// that disagrees mutes the kind. The value is NOT derived from data — no measurement exists that
    /// would separate a normal backlog from a real omission — so it is a starting point to be
    /// recalibrated against the kind's dismiss rate.
    /// </summary>
    public const int MinUngroupedShiftsForRecommendation = 10;

    private readonly IScheduleActivityProbe _activityProbe;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<UngroupedShiftsDetector> _logger;

    public UngroupedShiftsDetector(
        IScheduleActivityProbe activityProbe,
        ICompanyClock companyClock,
        ILogger<UngroupedShiftsDetector> logger)
    {
        _activityProbe = activityProbe;
        _companyClock = companyClock;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.UngroupedShifts;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var finding = await FindAsync(cancellationToken);
        if (finding == null)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        _logger.LogInformation(
            "UngroupedShifts scan: {Shifts} staffable duty/duties belong to no group, "
            + "recommending an assignment once",
            finding.UngroupedShiftCount);

        return [finding];
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var finding = await FindAsync(cancellationToken);
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);

        if (finding != null)
        {
            fingerprints.Add(AgentConditionLedgerPolicy.FingerprintFor(Kind, finding.DedupKey));
        }

        return fingerprints;
    }

    /// <summary>
    /// The single gate both paths run. The counting query is reached only once the cheap snapshot flag
    /// has already passed, so an installation without any group never pays for it.
    /// </summary>
    private async Task<UngroupedShiftsTriggerEvent?> FindAsync(CancellationToken cancellationToken)
    {
        var state = await _activityProbe.GetSetupStateAsync(cancellationToken);
        if (!state.HasGroups)
        {
            return null;
        }

        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var ungroupedShifts = await _activityProbe.CountUngroupedPlannableShiftsAsync(today, cancellationToken);

        return ungroupedShifts >= MinUngroupedShiftsForRecommendation
            ? new UngroupedShiftsTriggerEvent(ungroupedShifts)
            : null;
    }
}
