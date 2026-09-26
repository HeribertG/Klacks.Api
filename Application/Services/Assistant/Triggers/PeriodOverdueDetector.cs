// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects groups whose last completed pay period reached its close date OverdueDaysThreshold or more days
/// ago and is still not sealed (no SealedDay covering the period end). The close date is the period end plus
/// the optional PERIOD_CLOSE_LAG_DAYS setting; with no setting or a lag of 0 it is the period end itself and
/// the behaviour is exactly the one from before the setting existed. Period ends are computed
/// from the group's PaymentInterval exactly like PeriodCloseDueDetector, shifted one period
/// into the past; Individual is skipped and period ends before the group's ValidFrom are
/// ignored. Groups without any clients or shifts, in themselves or in a descendant group, are
/// skipped as well — an empty group has no period worth closing. Emits one
/// PeriodOverdueTriggerEvent per match.
///
/// Carrying shifts is not the same as having been planned: a group can hold hundreds of shift
/// definitions and still have no assignment in the period at all, and reminding someone to close
/// such a period is noise — there is nothing in it to close. Measured in the reference installation
/// on 2026-09-07: group "Deutschschweiz Ost" holds 420 shifts, 0 work rows exist installation-wide,
/// and 11 overdue dispatches had gone out for periods nobody ever planned. The probe therefore runs
/// as the LAST gate, after the sealed-day check, so it only costs a query for groups that would
/// otherwise have produced an event.
/// </summary>
/// <param name="groupRepository">Lists all groups (filters out deleted via query filter).</param>
/// <param name="sealedDayRepository">Used to check whether the period end is already sealed.</param>
/// <param name="weekConfiguration">Resolves the configured week start for weekly period ends.</param>
/// <param name="activityProbe">Answers whether the period holds any real work assignment at all.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="companyClock">Resolves "today" as the company's own local day, not the server's UTC day.</param>
/// <param name="settingsReader">Reads the optional PERIOD_CLOSE_LAG_DAYS setting.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class PeriodOverdueDetector : IAgentTriggerDetector
{
    private const int OverdueDaysThreshold = 7;

    private readonly IGroupRepository _groupRepository;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly IScheduleActivityProbe _activityProbe;
    private readonly ILogger<PeriodOverdueDetector> _logger;
    private readonly ICompanyClock _companyClock;
    private readonly ISettingsReader _settingsReader;

    public PeriodOverdueDetector(
        IGroupRepository groupRepository,
        ISealedDayRepository sealedDayRepository,
        IWeekConfiguration weekConfiguration,
        IScheduleActivityProbe activityProbe,
        ILogger<PeriodOverdueDetector> logger,
        ICompanyClock companyClock,
        ISettingsReader settingsReader)
    {
        _groupRepository = groupRepository;
        _sealedDayRepository = sealedDayRepository;
        _weekConfiguration = weekConfiguration;
        _activityProbe = activityProbe;
        _logger = logger;
        _companyClock = companyClock;
        _settingsReader = settingsReader;
    }

    public string Kind => AgentTriggerKinds.PeriodOverdue;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var groups = await _groupRepository.List();
        if (groups.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var lagDays = await PeriodCloseLagReader.ReadAsync(_settingsReader) ?? 0;
        var referenceDay = today.AddDays(-lagDays);
        var weekStart = await _weekConfiguration.GetWeekStartAsync(referenceDay, cancellationToken);
        var staffing = GroupStaffingLookup.Build(
            groups,
            await _groupRepository.GetGroupIdsWithMembersAsync(cancellationToken));

        var events = new List<IAgentTriggerEvent>();
        var skippedUnplanned = 0;
        foreach (var group in groups)
        {
            if (group.PaymentInterval == PaymentInterval.Individual) continue;
            if (!staffing.IsStaffed(group.Id)) continue;

            var periodEnd = PeriodBoundaries.LastEndBefore(group, referenceDay, weekStart);
            if (periodEnd < DateOnly.FromDateTime(group.ValidFrom)) continue;

            var daysOverdue = referenceDay.DayNumber - periodEnd.DayNumber;
            if (daysOverdue < OverdueDaysThreshold) continue;

            var existingSeals = await _sealedDayRepository.GetRangeAsync(periodEnd, periodEnd, group.Id, cancellationToken);
            if (existingSeals.Count > 0) continue;

            var periodStart = PeriodBoundaries.StartFor(group.PaymentInterval, periodEnd);
            if (!await _activityProbe.HasWorkInRangeAsync(group, periodStart, periodEnd, cancellationToken))
            {
                skippedUnplanned++;
                continue;
            }

            events.Add(new PeriodOverdueTriggerEvent(
                group.Id,
                group.Name,
                periodEnd,
                daysOverdue,
                lagDays));
        }

        _logger.LogInformation(
            "PeriodOverdue scan: {Total} group(s) scanned, {Events} overdue event(s) emitted, {SkippedUnplanned} skipped because the period holds no work",
            groups.Count, events.Count, skippedUnplanned);

        return events;
    }
}
