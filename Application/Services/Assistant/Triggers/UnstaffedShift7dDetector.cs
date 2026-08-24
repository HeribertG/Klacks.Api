// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans the next 7 days for shift-day assignments whose SumEmployees is below Quantity
/// (= still unstaffed) and emits one UnstaffedShiftTriggerEvent per shortfall. The
/// background scanner calls this every 60 minutes; the trigger service rate-limits
/// per user per UTC day so a single unfilled slot does not spam the user.
/// </summary>
/// <param name="shiftScheduleRepository">Returns ShiftDayAssignment rows with SumEmployees vs Quantity.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class UnstaffedShift7dDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private const int LookaheadDays = 7;
    private const int FilterRowCount = 1000;
    private const int UncappedRowCount = int.MaxValue;

    private readonly IShiftScheduleRepository _shiftScheduleRepository;
    private readonly ILogger<UnstaffedShift7dDetector> _logger;

    public UnstaffedShift7dDetector(
        IShiftScheduleRepository shiftScheduleRepository,
        ILogger<UnstaffedShift7dDetector> logger)
    {
        _shiftScheduleRepository = shiftScheduleRepository;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.UnstaffedShift;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = Today();

        var (assignments, _) = await _shiftScheduleRepository.GetShiftScheduleAsync(
            BuildFilter(today, FilterRowCount), cancellationToken);
        if (assignments.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = SelectUnstaffed(assignments, today)
            .Select(finding => (IAgentTriggerEvent)new UnstaffedShiftTriggerEvent(
                finding.Assignment.ShiftId,
                finding.Assignment.Date,
                finding.DaysUntil,
                null))
            .ToList();

        _logger.LogInformation(
            "UnstaffedShift7d scan: {Total} assignments scanned, {Events} unstaffed events emitted",
            assignments.Count, events.Count);

        return events;
    }

    /// <summary>
    /// Uses the identical filter and the identical unstaffed predicate as DetectAsync, only with the
    /// row cap lifted. That costs nothing at the database: ShiftScheduleRepository materialises the
    /// whole window first and applies RowCount afterwards, in memory, over unique shift ids. The
    /// LookaheadDays window itself stays - it is the kind's definition, not a cap, and it advances
    /// monotonically, so a finding only ever leaves it by ageing into the past, which is genuinely a
    /// resolution rather than a lost page.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var today = Today();

        var (assignments, _) = await _shiftScheduleRepository.GetShiftScheduleAsync(
            BuildFilter(today, UncappedRowCount), cancellationToken);

        return SelectUnstaffed(assignments, today)
            .Select(finding => AgentConditionLedgerPolicy.FingerprintFor(
                Kind,
                UnstaffedShiftTriggerEvent.DedupKeyFor(finding.Assignment.ShiftId, finding.Assignment.Date)))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static ShiftScheduleFilter BuildFilter(DateOnly today, int rowCount) => new()
    {
        StartDate = today,
        EndDate = today.AddDays(LookaheadDays),
        IsSporadic = true,
        IsTimeRange = true,
        Container = true,
        IsStandartShift = true,
        ShowUngroupedShifts = true,
        RowCount = rowCount
    };

    private static IEnumerable<(ShiftDayAssignment Assignment, int DaysUntil)> SelectUnstaffed(
        IEnumerable<ShiftDayAssignment> assignments,
        DateOnly today)
    {
        foreach (var assignment in assignments)
        {
            if (!UnstaffedShiftPredicate.IsUnstaffed(assignment)) continue;

            var daysUntil = assignment.Date.DayNumber - today.DayNumber;
            if (daysUntil < 0) continue;

            yield return (assignment, daysUntil);
        }
    }
}
