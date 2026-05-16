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

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class UnstaffedShift7dDetector : IAgentTriggerDetector
{
    private const int LookaheadDays = 7;
    private const int FilterRowCount = 1000;

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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var filter = new ShiftScheduleFilter
        {
            StartDate = today,
            EndDate = today.AddDays(LookaheadDays),
            IsSporadic = true,
            IsTimeRange = true,
            Container = true,
            IsStandartShift = true,
            ShowUngroupedShifts = true,
            RowCount = FilterRowCount
        };

        var (assignments, _) = await _shiftScheduleRepository.GetShiftScheduleAsync(filter, cancellationToken);
        if (assignments.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = new List<IAgentTriggerEvent>();
        foreach (var assignment in assignments)
        {
            if (assignment.Quantity <= 0) continue;
            if (assignment.SumEmployees >= assignment.Quantity) continue;

            var daysUntil = assignment.Date.DayNumber - today.DayNumber;
            if (daysUntil < 0) continue;

            events.Add(new UnstaffedShiftTriggerEvent(
                assignment.ShiftId,
                assignment.Date,
                daysUntil,
                null));
        }

        _logger.LogInformation(
            "UnstaffedShift7d scan: {Total} assignments scanned, {Events} unstaffed events emitted",
            assignments.Count, events.Count);

        return events;
    }
}
