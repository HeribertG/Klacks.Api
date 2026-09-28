// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Delivers the inbox entry of a chat-requested grouping analysis without duplicating the daily admin
/// report. For the default scope (whole installation, today plus the default horizon) the result becomes
/// today's snapshot, which the detector announces to all admins on its next hourly tick through the
/// condition ledger. The requester gets an entry of their own right away, unless they are an admin and
/// the snapshot was written (the detector reaches them then). The stored snapshot always covers the whole
/// analysis; the requester's entry uses the requester snapshot (counts, report findings and dedup key of
/// only what the requester's group scope shows), so a group-restricted user never sees installation-wide
/// numbers. Only visible report findings create an entry. The snapshot is written only if no apply removed
/// the day's snapshot since the caller captured the store generation before its analysis; otherwise the
/// pre-apply result stays out of the store and the requester (even an admin) gets an own entry.
/// </summary>
/// <param name="triggerService">Proactive dispatch pipeline.</param>
/// <param name="snapshotStore">Shared daily snapshot.</param>
/// <param name="companyClock">Company today, to recognise the default scope and key the snapshot.</param>
/// <param name="snapshotGeneration">Store generation captured before the analysis started.</param>
/// <param name="requesterSnapshot">Counts, report findings and fingerprint of the part of the report the requester may see.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupingFeasibilityNotifier : IGroupingFeasibilityNotifier
{
    private readonly IAgentTriggerService _triggerService;
    private readonly IGroupingFeasibilityDailySnapshotStore _snapshotStore;
    private readonly ICompanyClock _companyClock;

    public GroupingFeasibilityNotifier(
        IAgentTriggerService triggerService, IGroupingFeasibilityDailySnapshotStore snapshotStore, ICompanyClock companyClock)
    {
        _triggerService = triggerService;
        _snapshotStore = snapshotStore;
        _companyClock = companyClock;
    }

    public long CaptureSnapshotGeneration() => _snapshotStore.CurrentGeneration;

    public async Task NotifyAsync(
        GroupingFeasibilityReport report,
        GroupingFeasibilityDailySnapshot requesterSnapshot,
        Guid requesterId,
        bool requesterIsAdmin,
        long snapshotGeneration,
        CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var snapshot = GroupingFeasibilityDailySnapshot.From(report);
        var isDefaultScope = report.Request.ScopeGroupId is null
            && report.Request.From == today
            && report.Request.Until == today.AddDays(GroupingFeasibilityDefaults.DefaultHorizonDays);

        var stored = isDefaultScope && _snapshotStore.Set(GroupingFeasibilityDay.KeyFor(today), snapshot, snapshotGeneration);

        if (!requesterSnapshot.HasReportFindings || (stored && requesterIsAdmin))
        {
            return;
        }

        await _triggerService.OnEventAsync(
            new GroupingFeasibilityRequesterTriggerEvent(requesterSnapshot.ReportFingerprint, requesterSnapshot.Counts, requesterId),
            cancellationToken);
    }
}
