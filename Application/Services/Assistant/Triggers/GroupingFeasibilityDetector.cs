// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Daily grouping feasibility check for administrators. Runs on the shared hourly detector tick like
/// every detector; the rhythm is the company-day snapshot: the installation-wide analysis (company today
/// plus the default horizon, no subtree) runs at most once per company day and only when at least one
/// employee with a current membership and one grouped plannable shift exist. A new inbox entry appears
/// only when the report fingerprint changes; because the report is recomputed every company day, a report
/// the user fixed elsewhere stops being active (and its ledger row resolves) on the next company day.
/// Both DetectAsync and the fingerprint scan go through the same private gate, and the snapshot is also
/// held on the instance for the rest of the tick, so the fingerprint set contains the emitted event even
/// if the shared cache drops the entry in between. The set holds only the current report fingerprint (or
/// nothing), so the ledger resolves the old row as soon as the report findings change or disappear.
/// A computed snapshot is stored only if no apply removed the day's snapshot while the analysis ran
/// (store generation unchanged); otherwise the analysis is repeated once on the committed data, and if
/// that one loses the race too its result serves only this tick and is not stored.
/// </summary>
/// <param name="dataSource">Cheap data gate.</param>
/// <param name="analyzer">Full analysis, run at most once per company day.</param>
/// <param name="snapshotStore">Shared daily snapshot (also written by chat runs).</param>
/// <param name="companyClock">Company today for the day key and the analysis period.</param>
/// <param name="logger">One line per computed day.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class GroupingFeasibilityDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private const int MaxComputeAttempts = 2;

    private readonly IGroupingFeasibilityDataSource _dataSource;
    private readonly IGroupingFeasibilityAnalyzer _analyzer;
    private readonly IGroupingFeasibilityDailySnapshotStore _snapshotStore;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<GroupingFeasibilityDetector> _logger;

    private GroupingFeasibilityDailySnapshot? _tickSnapshot;

    public GroupingFeasibilityDetector(
        IGroupingFeasibilityDataSource dataSource,
        IGroupingFeasibilityAnalyzer analyzer,
        IGroupingFeasibilityDailySnapshotStore snapshotStore,
        ICompanyClock companyClock,
        ILogger<GroupingFeasibilityDetector> logger)
    {
        _dataSource = dataSource;
        _analyzer = analyzer;
        _snapshotStore = snapshotStore;
        _companyClock = companyClock;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.GroupingFeasibility;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var finding = await FindAsync(cancellationToken);
        return finding == null ? Array.Empty<IAgentTriggerEvent>() : [finding];
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var finding = await FindAsync(cancellationToken);
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        if (finding != null)
        {
            fingerprints.Add(AgentConditionLedgerPolicy.FingerprintFor(finding));
        }

        return fingerprints;
    }

    private async Task<GroupingFeasibilityTriggerEvent?> FindAsync(CancellationToken cancellationToken)
    {
        var snapshot = _tickSnapshot ?? await LoadOrComputeSnapshotAsync(cancellationToken);
        _tickSnapshot = snapshot;

        return snapshot is { HasReportFindings: true }
            ? new GroupingFeasibilityTriggerEvent(snapshot.ReportFingerprint, snapshot.Counts)
            : null;
    }

    private async Task<GroupingFeasibilityDailySnapshot?> LoadOrComputeSnapshotAsync(CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var dayKey = GroupingFeasibilityDay.KeyFor(today);
        GroupingFeasibilityDailySnapshot? snapshot = null;

        for (var attempt = 0; attempt < MaxComputeAttempts; attempt++)
        {
            var stored = _snapshotStore.TryGet(dayKey, out var generation);
            if (stored != null)
            {
                return stored;
            }

            if (!await _dataSource.HasAnalysableDataAsync(today, cancellationToken))
            {
                return null;
            }

            var report = await _analyzer.AnalyzeAsync(
                new GroupingAnalysisRequest(today, today.AddDays(GroupingFeasibilityDefaults.DefaultHorizonDays), null),
                cancellationToken);
            snapshot = GroupingFeasibilityDailySnapshot.From(report);
            if (_snapshotStore.Set(dayKey, snapshot, generation))
            {
                _logger.LogInformation(
                    "GroupingFeasibility day {Day}: {Shifts} unfillable duty/duties, {Clients} employee(s) without fit, "
                    + "{Capacity} capacity shortfall(s), {Proposals} proposal(s)",
                    dayKey, snapshot.Counts.UnfillableShifts, snapshot.Counts.UnmatchedClients,
                    snapshot.Counts.CapacityShortfalls, snapshot.Counts.Proposals);
                return snapshot;
            }

            _logger.LogInformation(
                "GroupingFeasibility day {Day}: a grouping plan was applied during the analysis; the result is not stored",
                dayKey);
        }

        return snapshot;
    }
}
