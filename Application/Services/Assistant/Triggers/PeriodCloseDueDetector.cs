// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects groups whose current pay-period end is within 3 days but the period is still open
/// (no SealedDay covering the end date). Period end is computed from the group's PaymentInterval:
/// Weekly = end of ISO week, Biweekly = end of 14-day window, Monthly = end of calendar month.
/// Individual is skipped (custom, no fixed cycle). Emits one PeriodCloseDueTriggerEvent per match.
/// </summary>
/// <param name="groupRepository">Lists all groups (filters out deleted via query filter).</param>
/// <param name="sealedDayRepository">Used to check whether the end date is already sealed.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class PeriodCloseDueDetector : IAgentTriggerDetector
{
    private const int WarnWithinDays = 3;

    private readonly IGroupRepository _groupRepository;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly ILogger<PeriodCloseDueDetector> _logger;
    private readonly TimeProvider _timeProvider;

    public PeriodCloseDueDetector(
        IGroupRepository groupRepository,
        ISealedDayRepository sealedDayRepository,
        ILogger<PeriodCloseDueDetector> logger,
        TimeProvider timeProvider)
    {
        _groupRepository = groupRepository;
        _sealedDayRepository = sealedDayRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public string Kind => AgentTriggerKinds.PeriodCloseDue;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var groups = await _groupRepository.List();
        if (groups.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = new List<IAgentTriggerEvent>();
        foreach (var group in groups)
        {
            if (group.PaymentInterval == PaymentInterval.Individual) continue;

            var periodEnd = ComputePeriodEnd(group, today);
            var daysUntil = periodEnd.DayNumber - today.DayNumber;
            if (daysUntil < 0 || daysUntil > WarnWithinDays) continue;

            var existingSeals = await _sealedDayRepository.GetRangeAsync(periodEnd, periodEnd, group.Id, cancellationToken);
            if (existingSeals.Count > 0) continue;

            events.Add(new PeriodCloseDueTriggerEvent(
                group.Id,
                group.Name,
                periodEnd,
                daysUntil));
        }

        _logger.LogInformation(
            "PeriodCloseDue scan: {Total} group(s) scanned, {Events} close-due events emitted",
            groups.Count, events.Count);

        return events;
    }

    private static DateOnly ComputePeriodEnd(Group group, DateOnly today)
    {
        return group.PaymentInterval switch
        {
            PaymentInterval.Weekly => EndOfIsoWeek(today),
            PaymentInterval.Biweekly => EndOfBiweekly(today, group.ValidFrom),
            PaymentInterval.Monthly => EndOfMonth(today),
            _ => throw new ArgumentOutOfRangeException(nameof(group),
                $"Unsupported PaymentInterval '{group.PaymentInterval}' — caller must filter Individual.")
        };
    }

    private static DateOnly EndOfMonth(DateOnly today)
    {
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        return new DateOnly(today.Year, today.Month, daysInMonth);
    }

    private static DateOnly EndOfIsoWeek(DateOnly today)
    {
        var dayOfWeek = (int)today.DayOfWeek;
        var daysUntilSunday = dayOfWeek == 0 ? 0 : 7 - dayOfWeek;
        return today.AddDays(daysUntilSunday);
    }

    private static DateOnly EndOfBiweekly(DateOnly today, DateTime groupAnchor)
    {
        var anchor = DateOnly.FromDateTime(groupAnchor);
        var daysSinceAnchor = today.DayNumber - anchor.DayNumber;
        var positionInCycle = ((daysSinceAnchor % 14) + 14) % 14;
        return today.AddDays(13 - positionInCycle);
    }
}
