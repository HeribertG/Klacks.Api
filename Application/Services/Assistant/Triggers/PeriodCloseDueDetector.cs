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
///
/// Also implements IAgentConditionFingerprintSource: GetActiveFingerprintsAsync reuses the same
/// window computation as DetectAsync, including the sealed-day check, but skips the activity check -
/// so its result is a deliberate SUPERSET of what DetectAsync emits with respect to that one guard
/// only (a still-unplanned group counts as an active fingerprint, a sealed one never does). That is
/// the safe direction — see IAgentConditionFingerprintSource's own summary for why a narrower set
/// would be the dangerous one.
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
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class PeriodCloseDueDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
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
        var (totalGroups, matches) = await FindGroupsWithPeriodEndInWindowAsync(cancellationToken);

        var events = new List<IAgentTriggerEvent>();
        var skippedUnplanned = 0;
        foreach (var match in matches)
        {
            var periodStart = PeriodBoundaries.StartFor(match.Group.PaymentInterval, match.PeriodEnd);
            if (!await _activityProbe.HasWorkInRangeAsync(match.Group, periodStart, match.PeriodEnd, cancellationToken))
            {
                skippedUnplanned++;
                continue;
            }

            events.Add(new PeriodCloseDueTriggerEvent(
                match.Group.Id,
                match.Group.Name,
                match.PeriodEnd,
                match.DaysUntilDue));
        }

        _logger.LogInformation(
            "PeriodCloseDue scan: {Total} group(s) scanned, {Events} close-due events emitted, {SkippedUnplanned} skipped because the period holds no work",
            totalGroups, events.Count, skippedUnplanned);

        return events;
    }

    /// <summary>
    /// Every fingerprint the window computation currently matches, deliberately WITHOUT the activity
    /// check DetectAsync applies afterwards - the activity probe is an anti-spam guard, not a truth
    /// condition, so a still-unplanned group must keep its fingerprint alive, or MarkResolvedAsync would
    /// resolve its ledger row on this very tick and re-arm it as new the moment the period gets planned.
    /// The sealed-day check, in contrast, IS the resolved condition itself and lives in the shared window
    /// computation below, so a sealed group drops out of both this set and DetectAsync's events together.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var (_, matches) = await FindGroupsWithPeriodEndInWindowAsync(cancellationToken);

        return matches
            .Select(match => AgentConditionLedgerPolicy.FingerprintFor(
                Kind, PeriodCloseDueTriggerEvent.DedupKeyFor(match.Group.Id, match.PeriodEnd)))
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// The window computation shared by DetectAsync and GetActiveFingerprintsAsync: which groups have a
    /// period end within WarnWithinDays days AND are not already sealed at that end date - a sealed
    /// period is the resolved condition itself, so both paths must drop it together, or a sealed group
    /// would linger in the fingerprint scan for up to WarnWithinDays days after it was actually closed.
    /// Whether the period holds any work is deliberately NOT part of this shared predicate; that check
    /// is DetectAsync's own anti-spam guard, never a truth condition about the group. Kept in ONE place
    /// so the two paths can never spell the window predicate, the staffing check or the dedup key
    /// differently from one another.
    /// </summary>
    private async Task<(int TotalGroups, IReadOnlyList<GroupPeriodEndMatch> Matches)> FindGroupsWithPeriodEndInWindowAsync(
        CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var groups = await _groupRepository.List();
        if (groups.Count == 0)
        {
            return (0, Array.Empty<GroupPeriodEndMatch>());
        }

        var weekStart = await _weekConfiguration.GetWeekStartAsync(today, cancellationToken);
        var endOfConfiguredWeek = weekStart.AddDays(6);
        var staffing = GroupStaffingLookup.Build(
            groups,
            await _groupRepository.GetGroupIdsWithMembersAsync(cancellationToken));

        var matches = new List<GroupPeriodEndMatch>();
        foreach (var group in groups)
        {
            if (group.PaymentInterval == PaymentInterval.Individual) continue;
            if (!staffing.IsStaffed(group.Id)) continue;

            var periodEnd = ComputePeriodEnd(group, today, endOfConfiguredWeek);
            var daysUntil = periodEnd.DayNumber - today.DayNumber;
            if (daysUntil < 0 || daysUntil > WarnWithinDays) continue;

            var existingSeals = await _sealedDayRepository.GetRangeAsync(periodEnd, periodEnd, group.Id, cancellationToken);
            if (existingSeals.Count > 0) continue;

            matches.Add(new GroupPeriodEndMatch(group, periodEnd, daysUntil));
        }

        return (groups.Count, matches);
    }

    /// <summary>
    /// One group whose period end falls inside the warn window and is not yet sealed, before the
    /// activity check that only DetectAsync applies.
    /// </summary>
    private sealed record GroupPeriodEndMatch(Group Group, DateOnly PeriodEnd, int DaysUntilDue);

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
