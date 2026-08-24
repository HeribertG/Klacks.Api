// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans Shift rows still in OriginalOrder status (an ERP-imported or manually created order not
/// yet sealed into a staffable shift) whose FromDate is today or later, and emits one
/// OpenOrderTriggerEvent per open order. Scenario clones (AnalyseToken/ScenarioSourceShiftId set)
/// and soft-deleted rows are excluded so a what-if scenario or a removed order never reaches the
/// real notification stream. No upper date bound: severity alone (High/Medium/Low) reflects how far
/// out FromDate lies -- this is a deliberate design choice, not something the
/// MaxCandidatesToScan cap below changes. Every emitted event carries the groups of its shift, read in
/// ONE batched lookup for the whole scan (never one query per order), because the group set is what
/// narrows the notification to the planners who may see that order. The scan is capped at MaxCandidatesToScan rows, ordered by
/// FromDate (soonest first, Id as tiebreaker) so the cap -- a defensive bound against unbounded
/// growth, mirroring UnstaffedShift7dDetector.FilterRowCount -- keeps exactly the highest-severity
/// candidates rather than an arbitrary storage-order subset. The background scanner calls this every
/// 60 minutes.
/// </summary>
/// <param name="shiftRepository">Read-only Shift query source.</param>
/// <param name="groupScopeReader">Batched shift-to-groups lookup for audience scoping.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class OpenOrderDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    public const int MaxCandidatesToScan = 1000;

    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftGroupScopeReader _groupScopeReader;
    private readonly ILogger<OpenOrderDetector> _logger;

    public OpenOrderDetector(
        IShiftRepository shiftRepository,
        IShiftGroupScopeReader groupScopeReader,
        ILogger<OpenOrderDetector> logger)
    {
        _shiftRepository = shiftRepository;
        _groupScopeReader = groupScopeReader;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.OpenOrder;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = Today();

        var openOrders = await BuildCandidateQuery(today)
            .OrderBy(s => s.FromDate)
            .ThenBy(s => s.Id)
            .Take(MaxCandidatesToScan)
            .ToListAsync(cancellationToken);

        if (openOrders.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var groupsByShift = await _groupScopeReader.GetGroupIdsByShiftIdsAsync(
            openOrders.Select(shift => shift.Id).ToList(), cancellationToken);

        var events = new List<IAgentTriggerEvent>();
        foreach (var shift in openOrders)
        {
            var daysUntil = shift.FromDate.DayNumber - today.DayNumber;

            events.Add(new OpenOrderTriggerEvent(
                shift.Id,
                shift.ClientId,
                shift.FromDate,
                shift.UntilDate,
                daysUntil,
                ShiftGroupScope.For(groupsByShift, shift.Id)));
        }

        _logger.LogInformation(
            "OpenOrder scan: {Total} open order(s) scanned, {Events} event(s) emitted",
            openOrders.Count, events.Count);

        return events;
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var keys = await BuildCandidateQuery(Today())
            .Select(s => new { s.Id, s.FromDate })
            .ToListAsync(cancellationToken);

        return keys
            .Select(key => AgentConditionLedgerPolicy.FingerprintFor(
                Kind,
                OpenOrderTriggerEvent.DedupKeyFor(key.Id, key.FromDate)))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private IQueryable<Shift> BuildCandidateQuery(DateOnly today) =>
        _shiftRepository.GetQuery()
            .Where(s => s.Status == ShiftStatus.OriginalOrder
                && s.FromDate >= today
                && s.AnalyseToken == null
                && s.ScenarioSourceShiftId == null
                && !s.IsDeleted);
}
