// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans Shift rows still in OriginalOrder status (an ERP-imported or manually created order not
/// yet sealed into a staffable shift) whose FromDate is today or later, and emits one
/// OpenOrderTriggerEvent per open order. Scenario clones (AnalyseToken/ScenarioSourceShiftId set)
/// and soft-deleted rows are excluded so a what-if scenario or a removed order never reaches the
/// real notification stream. No upper date bound: severity alone (High/Medium/Low) reflects how far
/// out FromDate lies. The background scanner calls this every 60 minutes.
/// </summary>
/// <param name="shiftRepository">Read-only Shift query source.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class OpenOrderDetector : IAgentTriggerDetector
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ILogger<OpenOrderDetector> _logger;

    public OpenOrderDetector(
        IShiftRepository shiftRepository,
        ILogger<OpenOrderDetector> logger)
    {
        _shiftRepository = shiftRepository;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.OpenOrder;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var openOrders = await _shiftRepository.GetQuery()
            .Where(s => s.Status == ShiftStatus.OriginalOrder
                && s.FromDate >= today
                && s.AnalyseToken == null
                && s.ScenarioSourceShiftId == null
                && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        if (openOrders.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = new List<IAgentTriggerEvent>();
        foreach (var shift in openOrders)
        {
            var daysUntil = shift.FromDate.DayNumber - today.DayNumber;

            events.Add(new OpenOrderTriggerEvent(
                shift.Id,
                shift.ClientId,
                shift.FromDate,
                shift.UntilDate,
                daysUntil));
        }

        _logger.LogInformation(
            "OpenOrder scan: {Total} open order(s) scanned, {Events} event(s) emitted",
            openOrders.Count, events.Count);

        return events;
    }
}
