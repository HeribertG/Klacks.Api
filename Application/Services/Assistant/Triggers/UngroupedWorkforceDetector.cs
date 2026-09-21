// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Notices that an installation plans a workforce of some size without a single group, and makes one
/// quiet recommendation about it. Three conditions must hold together:
///
/// (a) no group exists at all — read from the installation-wide setup snapshot, whose group probe is a
/// FLAT existence check; a nested-set scoped query would report the 364-of-438 groups carrying
/// Root = NULL as absent and the recommendation would fire in installations that are already organised;
///
/// (b) at least MinEmployeesForGroupingSuggestion employees are on the books today — below that, groups
/// buy nothing a filter cannot do, and a recommendation would be noise;
///
/// (c) something is actually being planned (HasWork). This is the exact inverse of no_schedule_yet's
/// own gate, so the two can never speak in the same tick: an installation that has never planned
/// anything needs the first schedule, not a grouping, and hearing both at once would put two different
/// "start here" messages in front of the same administrator.
///
/// Emits at most ONE event per tick — the condition is a single installation-wide fact, not a row per
/// entity — and the fingerprint scan runs the IDENTICAL gate through the same private helper rather
/// than re-stating it. That is what makes the fingerprint set COMPLETE in both directions: the ledger
/// row resolves itself the moment a group appears, and equally when the workforce shrinks below the
/// threshold or the schedule is emptied. Re-stating the gate in two places is the failure mode here,
/// because a fingerprint scan narrower than the emission gate resolves rows the same tick opened them.
/// </summary>
/// <param name="activityProbe">Installation-wide setup snapshot and the active-employee headcount.</param>
/// <param name="companyClock">Resolves "today" as the company's own local day for the membership window.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class UngroupedWorkforceDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    /// <summary>
    /// The headcount from which groups start paying for themselves. Class-local like
    /// EmptyContainerDetector.MaxFindingsPerTick rather than a configuration setting: it is a judgement
    /// about when advice is worth giving, and an installation that disagrees mutes the kind.
    /// </summary>
    public const int MinEmployeesForGroupingSuggestion = 15;

    private readonly IScheduleActivityProbe _activityProbe;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<UngroupedWorkforceDetector> _logger;

    public UngroupedWorkforceDetector(
        IScheduleActivityProbe activityProbe,
        ICompanyClock companyClock,
        ILogger<UngroupedWorkforceDetector> logger)
    {
        _activityProbe = activityProbe;
        _companyClock = companyClock;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.UngroupedWorkforce;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var finding = await FindAsync(cancellationToken);
        if (finding == null)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        _logger.LogInformation(
            "UngroupedWorkforce scan: {Employees} active employee(s) are planned without any group, "
            + "recommending a grouping once",
            finding.ActiveEmployeeCount);

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
    /// The single gate both paths run. The headcount query is reached only once the two cheap snapshot
    /// flags have already passed, so an installation with groups never pays for it.
    /// </summary>
    private async Task<UngroupedWorkforceTriggerEvent?> FindAsync(CancellationToken cancellationToken)
    {
        var state = await _activityProbe.GetSetupStateAsync(cancellationToken);
        if (state.HasGroups || !state.HasWork)
        {
            return null;
        }

        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var activeEmployees = await _activityProbe.CountActiveEmployeesAsync(today, cancellationToken);

        return activeEmployees >= MinEmployeesForGroupingSuggestion
            ? new UngroupedWorkforceTriggerEvent(activeEmployees)
            : null;
    }
}
