// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects groups whose current pay-period end is within 3 days but the period is still open
/// (no SealedDay covering the end date). Period end is computed from the group's PaymentInterval:
/// Weekly = end of the configured business week, Biweekly = end of 14-day window, Monthly and
/// MonthlyTargetHours = end of calendar month. Individual is skipped (custom, no fixed cycle), as are groups without any
/// clients or shifts in themselves or in a descendant group. Emits one PeriodCloseDueTriggerEvent per match.
///
/// A group that holds shifts has not necessarily been planned: when the period contains no work
/// assignment at all there is nothing in it to close, and the reminder is noise. The probe runs as
/// the LAST gate, after the sealed-day check, so it only costs a query for groups that would
/// otherwise have produced an event. Same defect class as PeriodOverdueDetector — see its summary
/// for the measured case.
/// </summary>
/// <param name="groupRepository">Lists all groups (filters out deleted via query filter).</param>
/// <param name="sealedDayRepository">Used to check whether the end date is already sealed.</param>
/// <param name="weekConfiguration">Resolves the configured week start for weekly period ends.</param>
/// <param name="activityProbe">Answers whether the period holds any real work assignment at all.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="companyClock">Resolves "today" as the company's own local day, not the server's UTC day.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class PeriodCloseDueDetector : IAgentTriggerDetector
{
    private const int WarnWithinDays = 3;

    private readonly IGroupRepository _groupRepository;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly IScheduleActivityProbe _activityProbe;
    private readonly ILogger<PeriodCloseDueDetector> _logger;
    private readonly ICompanyClock _companyClock;

    public PeriodCloseDueDetector(
        IGroupRepository groupRepository,
        ISealedDayRepository sealedDayRepository,
        IWeekConfiguration weekConfiguration,
        IScheduleActivityProbe activityProbe,
        ILogger<PeriodCloseDueDetector> logger,
        ICompanyClock companyClock)
    {
        _groupRepository = groupRepository;
        _sealedDayRepository = sealedDayRepository;
        _weekConfiguration = weekConfiguration;
        _activityProbe = activityProbe;
        _logger = logger;
        _companyClock = companyClock;
    }

    public string Kind => AgentTriggerKinds.PeriodCloseDue;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var groups = await _groupRepository.List();
        if (groups.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var weekStart = await _weekConfiguration.GetWeekStartAsync(today, cancellationToken);
        var endOfConfiguredWeek = weekStart.AddDays(6);
        var staffing = GroupStaffingLookup.Build(
            groups,
            await _groupRepository.GetGroupIdsWithMembersAsync(cancellationToken));

        var events = new List<IAgentTriggerEvent>();
        var skippedUnplanned = 0;
        foreach (var group in groups)
        {
            if (group.PaymentInterval == PaymentInterval.Individual) continue;
            if (!staffing.IsStaffed(group.Id)) continue;

            var periodEnd = ComputePeriodEnd(group, today, endOfConfiguredWeek);
            var daysUntil = periodEnd.DayNumber - today.DayNumber;
            if (daysUntil < 0 || daysUntil > WarnWithinDays) continue;

            var existingSeals = await _sealedDayRepository.GetRangeAsync(periodEnd, periodEnd, group.Id, cancellationToken);
            if (existingSeals.Count > 0) continue;

            var periodStart = PeriodBoundaries.StartFor(group.PaymentInterval, periodEnd);
            if (!await _activityProbe.HasWorkInRangeAsync(group, periodStart, periodEnd, cancellationToken))
            {
                skippedUnplanned++;
                continue;
            }

            events.Add(new PeriodCloseDueTriggerEvent(
                group.Id,
                group.Name,
                periodEnd,
                daysUntil));
        }

        _logger.LogInformation(
            "PeriodCloseDue scan: {Total} group(s) scanned, {Events} close-due events emitted, {SkippedUnplanned} skipped because the period holds no work",
            groups.Count, events.Count, skippedUnplanned);

        return events;
    }

    private static DateOnly ComputePeriodEnd(Group group, DateOnly today, DateOnly endOfConfiguredWeek)
    {
        return group.PaymentInterval switch
        {
            PaymentInterval.Weekly => endOfConfiguredWeek,
            PaymentInterval.Biweekly => EndOfBiweekly(today, group.ValidFrom),
            PaymentInterval.Monthly => EndOfMonth(today),
            PaymentInterval.MonthlyTargetHours => EndOfMonth(today),
            _ => throw new ArgumentOutOfRangeException(nameof(group),
                $"Unsupported PaymentInterval '{group.PaymentInterval}' — caller must filter Individual.")
        };
    }

    private static DateOnly EndOfMonth(DateOnly today)
    {
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        return new DateOnly(today.Year, today.Month, daysInMonth);
    }

    private static DateOnly EndOfBiweekly(DateOnly today, DateTime groupAnchor)
    {
        var anchor = DateOnly.FromDateTime(groupAnchor);
        var daysSinceAnchor = today.DayNumber - anchor.DayNumber;
        var positionInCycle = ((daysSinceAnchor % 14) + 14) % 14;
        return today.AddDays(13 - positionInCycle);
    }
}
