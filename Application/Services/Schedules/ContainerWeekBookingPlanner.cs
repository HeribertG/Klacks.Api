// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides, date by date, where assign_container_week may book an employee on a container. A date is booked when the
/// container is valid and runs on its weekday, a template applies to it (ContainerTemplateForDate, the pick the work
/// expansion makes), the employee has no work on the container that day yet, the day is not sealed for the employee,
/// the day lies inside the employee's planning period and the pre-commit check reports no blocking finding. Every
/// other date is left out with its reason; non-blocking findings of the booked dates are returned as warnings. Writes
/// nothing, and the filters run in that order, so each date carries the first reason that applies.
/// </summary>
/// <param name="workRepository">Reads the employee's existing works</param>
/// <param name="sealedDayRepository">Tells which days are sealed for the employee</param>
/// <param name="planningPeriodGuard">Names the days outside the employee's membership or group period</param>
/// <param name="conflictChecker">Reports the schedule findings a booking would introduce</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ContainerWeekBookingPlanner
{
    private const string BeforeValidityReason = "the container is valid only from {0}";
    private const string AfterValidityReason = "the container is valid only until {0}";
    private const string NotActiveReason = "the container does not run on {0}";
    private const string NoTemplateReason = "the container has no template for {0}";
    private const string AlreadyBookedReason = "the employee is already booked on this container that day";
    private const string SealedDayReason = "the day is sealed for the employee";
    private const string BlockingConflictReason = "it would introduce a blocking schedule conflict ({0})";
    private const string CommentSeparator = ", ";

    private readonly IWorkRepository _workRepository;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IClientPlanningPeriodGuard _planningPeriodGuard;
    private readonly IPreCommitConflictChecker _conflictChecker;

    public ContainerWeekBookingPlanner(
        IWorkRepository workRepository,
        ISealedDayRepository sealedDayRepository,
        IClientPlanningPeriodGuard planningPeriodGuard,
        IPreCommitConflictChecker conflictChecker)
    {
        _workRepository = workRepository;
        _sealedDayRepository = sealedDayRepository;
        _planningPeriodGuard = planningPeriodGuard;
        _conflictChecker = conflictChecker;
    }

    /// <param name="container">The plannable container shift the works are booked on</param>
    /// <param name="templates">The templates of that container</param>
    /// <param name="clientId">The employee to book</param>
    /// <param name="from">First date of the range</param>
    /// <param name="until">Last date of the range</param>
    public async Task<ContainerWeekPlan> PlanAsync(
        Shift container,
        IReadOnlyList<ContainerTemplate> templates,
        Guid clientId,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken)
    {
        var skipped = new List<ContainerWeekSkip>();
        var existing = await ExistingBookingDatesAsync(container.Id, clientId, from, until, cancellationToken);
        var candidates = new List<ContainerWeekBooking>();

        for (var date = from; date <= until; date = date.AddDays(1))
        {
            var (booking, reason) = Classify(container, templates, existing, date);
            if (booking is null)
            {
                skipped.Add(new ContainerWeekSkip(date, reason!));
                continue;
            }

            candidates.Add(booking);
        }

        candidates = await DropSealedDaysAsync(clientId, candidates, skipped, cancellationToken);
        candidates = await DropOutsidePeriodAsync(clientId, candidates, skipped, cancellationToken);
        var (booked, warnings) = await DropBlockedAsync(container, clientId, candidates, skipped, cancellationToken);

        return new ContainerWeekPlan(booked, skipped.OrderBy(s => s.Date).ToList(), warnings);
    }

    private async Task<HashSet<DateOnly>> ExistingBookingDatesAsync(
        Guid containerId, Guid clientId, DateOnly from, DateOnly until, CancellationToken cancellationToken)
    {
        var works = await _workRepository.GetByClientAndDateRangeAsync(
            clientId,
            from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            until.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            cancellationToken);

        return MainPlanWorks.ParentsOnly(works)
            .Where(work => work.ShiftId == containerId)
            .Select(work => work.CurrentDate)
            .ToHashSet();
    }

    private static (ContainerWeekBooking? Booking, string? Reason) Classify(
        Shift container, IReadOnlyList<ContainerTemplate> templates, HashSet<DateOnly> existing, DateOnly date)
    {
        if (date < container.FromDate)
        {
            return (null, string.Format(BeforeValidityReason, FormatDate(container.FromDate)));
        }

        if (container.UntilDate.HasValue && date > container.UntilDate.Value)
        {
            return (null, string.Format(AfterValidityReason, FormatDate(container.UntilDate.Value)));
        }

        if (!WeekdayFlags.Of(container).IsoDays().Contains(IsoWeekday.FromStorage((int)date.DayOfWeek)))
        {
            return (null, string.Format(NotActiveReason, WeekdayName(date)));
        }

        var template = ContainerTemplateForDate.Pick(templates, date);
        if (template is null)
        {
            return (null, string.Format(NoTemplateReason, WeekdayName(date)));
        }

        if (existing.Contains(date))
        {
            return (null, AlreadyBookedReason);
        }

        return (new ContainerWeekBooking(date, template.ContainerTemplateItems.Count(item => item.ShiftId.HasValue)), null);
    }

    private async Task<List<ContainerWeekBooking>> DropSealedDaysAsync(
        Guid clientId, List<ContainerWeekBooking> candidates, List<ContainerWeekSkip> skipped, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return candidates;
        }

        var sealedPairs = await _sealedDayRepository.GetLockedPairsAsync(
            candidates.Select(c => (c.Date, clientId)).ToList(), cancellationToken);

        skipped.AddRange(candidates.Where(c => sealedPairs.Contains((c.Date, clientId)))
            .Select(c => new ContainerWeekSkip(c.Date, SealedDayReason)));
        return candidates.Where(c => !sealedPairs.Contains((c.Date, clientId))).ToList();
    }

    private async Task<List<ContainerWeekBooking>> DropOutsidePeriodAsync(
        Guid clientId, List<ContainerWeekBooking> candidates, List<ContainerWeekSkip> skipped, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return candidates;
        }

        var violations = await _planningPeriodGuard.FindViolationsAsync(
            candidates.Select(c => (clientId, c.Date)).ToList(), cancellationToken);
        var violationByDate = violations.GroupBy(v => v.Date).ToDictionary(g => g.Key, g => g.First().Message);

        skipped.AddRange(candidates.Where(c => violationByDate.ContainsKey(c.Date))
            .Select(c => new ContainerWeekSkip(c.Date, violationByDate[c.Date])));
        return candidates.Where(c => !violationByDate.ContainsKey(c.Date)).ToList();
    }

    private async Task<(List<ContainerWeekBooking> Booked, List<ContainerWeekWarning> Warnings)> DropBlockedAsync(
        Shift container,
        Guid clientId,
        List<ContainerWeekBooking> candidates,
        List<ContainerWeekSkip> skipped,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return (candidates, []);
        }

        var rows = candidates
            .Select(c => new PlannedWorkRow(clientId, c.Date, container.StartShift, container.EndShift, container.Id))
            .ToList();
        var check = await _conflictChecker.CheckAsync(rows, null, cancellationToken);

        var blockedByDate = check.NewConflicts
            .Where(conflict => conflict.Type == ScheduleValidationType.Error)
            .GroupBy(conflict => conflict.Date)
            .ToDictionary(g => g.Key, g => string.Join(CommentSeparator, g.Select(conflict => conflict.Comment).Distinct()));

        skipped.AddRange(candidates.Where(c => blockedByDate.ContainsKey(c.Date))
            .Select(c => new ContainerWeekSkip(c.Date, string.Format(BlockingConflictReason, blockedByDate[c.Date]))));

        var booked = candidates.Where(c => !blockedByDate.ContainsKey(c.Date)).ToList();
        var bookedDates = booked.Select(c => c.Date).ToHashSet();
        var warnings = check.NewConflicts
            .Where(conflict => conflict.Type != ScheduleValidationType.Error && bookedDates.Contains(conflict.Date))
            .Select(conflict => new ContainerWeekWarning(conflict.Date, conflict.Type.ToString(), conflict.Comment))
            .ToList();

        return (booked, warnings);
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);

    private static string WeekdayName(DateOnly date) =>
        CultureInfo.InvariantCulture.DateTimeFormat.GetDayName(date.DayOfWeek);
}
