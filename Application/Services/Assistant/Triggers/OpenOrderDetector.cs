// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Collects every open order of the real plan and emits at most ONE OpenOrdersSummaryTriggerEvent per tick,
/// which AgentTriggerService narrows to one summary per planner (the first of the day, then only when that planner's
/// count rises - see OpenOrdersSummaryTriggerEvent). An order is open while it is still an
/// unsealed OriginalOrder (an ERP-imported or manually created order not yet sealed into a staffable shift) and
/// has not ended: UntilDate is empty or on or after the company's today. Scenario clones and soft-deleted rows
/// never count; the real-plan filters are the ones IShiftReader applies to every proactive scan.
///
/// The scan is uncapped on purpose: the message states a count, and a cap would make it wrong. It reads only
/// the order keys (id and first day) plus the group memberships of all of them in ONE batched lookup, so the
/// cost grows with the number of open orders, not with their width. Counting happens per recipient in memory
/// when AgentTriggerService narrows the event to the groups each planner may see - the same audience rule
/// the former per-order messages used, so nobody's view of which orders exist changes.
///
/// The background tick calls DetectAsync and GetActiveFingerprintsAsync on the same scoped instance, so the scan
/// is memoised: the fingerprint set is derived from the very scan the event came from (set equality, never a
/// narrower set). It holds today's single fingerprint while any order is open and nothing otherwise, which also
/// resolves yesterday's row and every per-order row of the former shape.
/// </summary>
/// <param name="shiftReader">Uncapped key scan of the real-plan open orders.</param>
/// <param name="groupScopeReader">Batched shift-to-groups lookup for the per-recipient narrowing.</param>
/// <param name="companyClock">Resolves "today" as the company's own local day, not the server's UTC day.</param>
/// <param name="logger">Structured log per scan.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class OpenOrderDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private readonly IShiftReader _shiftReader;
    private readonly IShiftGroupScopeReader _groupScopeReader;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<OpenOrderDetector> _logger;

    private Task<OpenOrdersSummaryTriggerEvent?>? _scan;

    public OpenOrderDetector(
        IShiftReader shiftReader,
        IShiftGroupScopeReader groupScopeReader,
        ICompanyClock companyClock,
        ILogger<OpenOrderDetector> logger)
    {
        _shiftReader = shiftReader;
        _groupScopeReader = groupScopeReader;
        _companyClock = companyClock;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.OpenOrder;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var summary = await ScanOnceAsync(cancellationToken);
        return summary is null ? Array.Empty<IAgentTriggerEvent>() : [summary];
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var summary = await ScanOnceAsync(cancellationToken);
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        if (summary is not null)
        {
            fingerprints.Add(AgentConditionLedgerPolicy.FingerprintFor(summary));
        }

        return fingerprints;
    }

    private Task<OpenOrdersSummaryTriggerEvent?> ScanOnceAsync(CancellationToken cancellationToken) =>
        _scan ??= ScanAsync(cancellationToken);

    private async Task<OpenOrdersSummaryTriggerEvent?> ScanAsync(CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);

        var openOrders = await _shiftReader.ScanRealPlanKeysAsync(
            new RealPlanShiftScan(ShiftStatus.OriginalOrder) { NotEndedBefore = today },
            cancellationToken);

        if (openOrders.Count == 0)
        {
            _logger.LogInformation("OpenOrder scan: no open order");
            return null;
        }

        var groupsByShift = await _groupScopeReader.GetGroupIdsByShiftIdsAsync(
            openOrders.Select(order => order.Id).ToList(), cancellationToken);

        var summary = new OpenOrdersSummaryTriggerEvent(
            today,
            openOrders
                .Select(order => new OpenOrderSnapshot(order.Id, order.FromDate, ShiftGroupScope.For(groupsByShift, order.Id)))
                .ToList());

        _logger.LogInformation(
            "OpenOrder scan: {Total} open order(s), {Soon} starting within {Window} days",
            summary.Count, summary.SoonCount, OpenOrdersSummaryTriggerEvent.SoonWindowDays);

        return summary;
    }
}
