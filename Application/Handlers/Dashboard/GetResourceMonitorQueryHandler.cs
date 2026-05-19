// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for computing per-day employee headcount for the resource monitor dashboard card.
/// Mixed units on a single Mitarbeiter (MA) scale:
///   MaxKapazitaet = sum of per-employee FTE share. 24/7 contracts (all WorkOn flags) are smoothed
///                   uniformly across 7 days; restricted patterns (e.g. Mo-Fr) follow the realistic
///                   weekday distribution.
///   Dienste       = number of shifts scheduled on date (each shift = 1 employee position).
///                   Container shifts count as 1; the task shifts inside them (referenced via
///                   ContainerTemplateItem) are excluded to avoid double-counting.
///   Absenzen      = sum of Absence.DefaultValue per active BreakPlaceholder, taken literally as
///                   entered (vacation and sickness include weekends — no FTE weighting, no weekday filter).
/// </summary>
/// <param name="context">Database context for ClientContract, Shift, BreakPlaceholder, GroupItem, and Settings queries</param>
/// <param name="logger">Logger for error handling via BaseHandler</param>
using System.Globalization;
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetResourceMonitorQueryHandler : BaseHandler, IRequestHandler<GetResourceMonitorQuery, ResourceMonitorResource>
{
    private const double FALLBACK_FULL_DAY_HOURS = 8.0;

    private readonly DataBaseContext _context;

    public GetResourceMonitorQueryHandler(DataBaseContext context, ILogger<GetResourceMonitorQueryHandler> logger)
        : base(logger)
    {
        _context = context;
    }

    public async Task<ResourceMonitorResource> Handle(GetResourceMonitorQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var startDate = new DateOnly(request.Year, 1, 1);
            var endDate = new DateOnly(request.Year, 12, 31);

            var fullDayHours = await ReadDefaultWorkingHoursAsync(cancellationToken);

            HashSet<Guid>? groupShiftIds = null;
            if (request.GroupId.HasValue)
            {
                groupShiftIds = await _context.GroupItem
                    .Where(gi => gi.GroupId == request.GroupId && !gi.IsDeleted && gi.ShiftId != null)
                    .Select(gi => gi.ShiftId!.Value)
                    .ToHashSetAsync(cancellationToken);
            }

            HashSet<Guid>? groupClientIds = null;
            if (groupShiftIds != null)
            {
                groupClientIds = await _context.Work
                    .Where(w => !w.IsDeleted
                        && w.CurrentDate >= startDate
                        && w.CurrentDate <= endDate
                        && w.AnalyseToken == null
                        && groupShiftIds.Contains(w.ShiftId))
                    .Select(w => w.ClientId)
                    .Distinct()
                    .ToHashSetAsync(cancellationToken);
            }

            var contractQuery = _context.ClientContract
                .Include(cc => cc.Contract)
                .Where(cc => !cc.IsDeleted
                    && cc.IsActive
                    && cc.FromDate <= endDate
                    && (cc.UntilDate == null || cc.UntilDate >= startDate));

            if (groupClientIds != null)
                contractQuery = contractQuery.Where(cc => groupClientIds.Contains(cc.ClientId));

            var contracts = await contractQuery.ToListAsync(cancellationToken);

            var contractsByClient = contracts
                .GroupBy(cc => cc.ClientId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var containedShiftIds = await _context.ContainerTemplateItem
                .Where(cti => !cti.IsDeleted
                    && cti.ShiftId != null
                    && !cti.ContainerTemplate.IsDeleted)
                .Select(cti => cti.ShiftId!.Value)
                .Distinct()
                .ToHashSetAsync(cancellationToken);

            var shiftQuery = _context.Shift
                .Where(s => !s.IsDeleted
                    && !s.IsTimeRange
                    && !s.IsSporadic
                    && s.AnalyseToken == null
                    && s.Status != ShiftStatus.SealedOrder
                    && !containedShiftIds.Contains(s.Id)
                    && s.FromDate <= endDate
                    && (s.UntilDate == null || s.UntilDate >= startDate));

            if (groupShiftIds != null)
                shiftQuery = shiftQuery.Where(s => groupShiftIds.Contains(s.Id));

            var shifts = await shiftQuery
                .Select(s => new
                {
                    s.FromDate,
                    s.UntilDate,
                    s.IsMonday,
                    s.IsTuesday,
                    s.IsWednesday,
                    s.IsThursday,
                    s.IsFriday,
                    s.IsSaturday,
                    s.IsSunday,
                })
                .ToListAsync(cancellationToken);

            var periodStart = startDate.ToDateTime(TimeOnly.MinValue);
            var periodEnd   = endDate.ToDateTime(TimeOnly.MaxValue);

            var absenzQuery = _context.BreakPlaceholder
                .Include(bp => bp.Absence)
                .Where(bp => !bp.IsDeleted
                    && bp.From < periodEnd
                    && bp.Until > periodStart);

            if (groupClientIds != null)
                absenzQuery = absenzQuery.Where(bp => groupClientIds.Contains(bp.ClientId));

            var absences = await absenzQuery
                .Select(bp => new { bp.From, bp.Until, bp.ClientId, DefaultValue = bp.Absence.DefaultValue })
                .ToListAsync(cancellationToken);

            var absenzByDate = new Dictionary<DateOnly, double>();
            foreach (var bp in absences)
            {
                if (bp.DefaultValue <= 0) continue;

                var fromDay = DateOnly.FromDateTime(bp.From);
                var untilDay = DateOnly.FromDateTime(bp.Until);
                if (fromDay < startDate) fromDay = startDate;
                if (untilDay > endDate) untilDay = endDate;

                for (var d = fromDay; d <= untilDay; d = d.AddDays(1))
                {
                    absenzByDate[d] = absenzByDate.GetValueOrDefault(d) + bp.DefaultValue;
                }
            }

            var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
            var dailyData = new List<ResourceMonitorDayResource>(totalDays);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                double maxKapaCount = 0;
                foreach (var (_, clientContracts) in contractsByClient)
                {
                    maxKapaCount += ComputeFteShareForClient(clientContracts, date, fullDayHours);
                }

                double dienstCount = 0;
                foreach (var s in shifts)
                {
                    if (s.FromDate > date || (s.UntilDate.HasValue && s.UntilDate.Value < date)) continue;
                    if (IsWeekdayActive(date, s.IsMonday, s.IsTuesday, s.IsWednesday, s.IsThursday, s.IsFriday, s.IsSaturday, s.IsSunday))
                        dienstCount += 1;
                }

                absenzByDate.TryGetValue(date, out var absenzCount);

                dailyData.Add(new ResourceMonitorDayResource
                {
                    Date = date,
                    MaxKapazitaetCount = Math.Round(maxKapaCount, 2),
                    DienstCount = dienstCount,
                    AbsenzCount = Math.Round(absenzCount, 2),
                });
            }

            return new ResourceMonitorResource { DailyData = dailyData };
        }, nameof(Handle));
    }

    private async Task<double> ReadDefaultWorkingHoursAsync(CancellationToken cancellationToken)
    {
        var raw = await _context.Settings
            .Where(s => s.Type == Klacks.Api.Application.Constants.Settings.DEFAULT_WORKING_HOURS)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(raw)
            && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0)
        {
            return parsed;
        }

        return FALLBACK_FULL_DAY_HOURS;
    }

    private static double ComputeFteShareForClient(List<ClientContract> clientContracts, DateOnly date, double fullDayHours)
    {
        const int FULL_WEEK_DAYS = 7;

        double dailyHours = 0;
        foreach (var cc in clientContracts)
        {
            if (cc.Contract is null) continue;
            if (cc.FromDate > date || (cc.UntilDate.HasValue && cc.UntilDate.Value < date)) continue;
            if (!cc.Contract.GuaranteedHours.HasValue || cc.Contract.GuaranteedHours.Value <= 0) continue;

            var flaggedDays =
                (cc.Contract.WorkOnMonday    ? 1 : 0) +
                (cc.Contract.WorkOnTuesday   ? 1 : 0) +
                (cc.Contract.WorkOnWednesday ? 1 : 0) +
                (cc.Contract.WorkOnThursday  ? 1 : 0) +
                (cc.Contract.WorkOnFriday    ? 1 : 0) +
                (cc.Contract.WorkOnSaturday  ? 1 : 0) +
                (cc.Contract.WorkOnSunday    ? 1 : 0);

            if (flaggedDays == 0) continue;

            var weeklyH = cc.Contract.PaymentInterval switch
            {
                PaymentInterval.Weekly   => (double)cc.Contract.GuaranteedHours.Value,
                PaymentInterval.Biweekly => (double)cc.Contract.GuaranteedHours.Value / 2.0,
                PaymentInterval.Monthly  => (double)cc.Contract.GuaranteedHours.Value * 12.0 / 52.0,
                _                        => (double)cc.Contract.GuaranteedHours.Value * 12.0 / 52.0,
            };

            if (flaggedDays == FULL_WEEK_DAYS)
            {
                // 24/7 contract: smoothed average across all calendar days (we don't know which days
                // the employee actually rests, so distribute uniformly).
                dailyHours += weeklyH / FULL_WEEK_DAYS;
            }
            else if (IsWeekdayActive(date,
                cc.Contract.WorkOnMonday, cc.Contract.WorkOnTuesday, cc.Contract.WorkOnWednesday,
                cc.Contract.WorkOnThursday, cc.Contract.WorkOnFriday,
                cc.Contract.WorkOnSaturday, cc.Contract.WorkOnSunday))
            {
                // Restricted weekday pattern (e.g., Mo-Fr): realistic — full daily hours on flagged days, 0 on rest days.
                dailyHours += weeklyH / flaggedDays;
            }
        }

        return fullDayHours > 0 ? dailyHours / fullDayHours : 0;
    }

    private static bool IsWeekdayActive(
        DateOnly date,
        bool mon, bool tue, bool wed, bool thu, bool fri, bool sat, bool sun) => date.DayOfWeek switch
        {
            DayOfWeek.Monday    => mon,
            DayOfWeek.Tuesday   => tue,
            DayOfWeek.Wednesday => wed,
            DayOfWeek.Thursday  => thu,
            DayOfWeek.Friday    => fri,
            DayOfWeek.Saturday  => sat,
            DayOfWeek.Sunday    => sun,
            _                   => false,
        };
}
