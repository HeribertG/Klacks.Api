// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for computing per-day capacity, service, and absence hours for the resource monitor dashboard card.
/// Symmetric calculation: MaxKapazitaet sums per-employee daily contract hours; AbsenzHours subtracts the
/// same daily hours weighted by Absence.DefaultValue (1.0 = full day, 0.5 = half day) when a BreakPlaceholder
/// is active for that employee. Dienste are derived from Shift schedules independently.
/// </summary>
/// <param name="context">Database context for ClientContract, Shift, BreakPlaceholder, and GroupItem queries</param>
/// <param name="logger">Logger for error handling via BaseHandler</param>
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetResourceMonitorQueryHandler : BaseHandler, IRequestHandler<GetResourceMonitorQuery, ResourceMonitorResource>
{
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

            var shiftQuery = _context.Shift
                .Where(s => !s.IsDeleted
                    && !s.IsTimeRange
                    && !s.IsSporadic
                    && s.AnalyseToken == null
                    && s.Status != ShiftStatus.SealedOrder
                    && s.FromDate <= endDate
                    && (s.UntilDate == null || s.UntilDate >= startDate));

            if (groupShiftIds != null)
                shiftQuery = shiftQuery.Where(s => groupShiftIds.Contains(s.Id));

            var shifts = await shiftQuery
                .Select(s => new
                {
                    s.FromDate,
                    s.UntilDate,
                    s.WorkTime,
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
                if (!contractsByClient.TryGetValue(bp.ClientId, out var clientContracts)) continue;

                var fromDay = DateOnly.FromDateTime(bp.From);
                var untilDay = DateOnly.FromDateTime(bp.Until);
                if (fromDay < startDate) fromDay = startDate;
                if (untilDay > endDate) untilDay = endDate;

                for (var d = fromDay; d <= untilDay; d = d.AddDays(1))
                {
                    var dailyHours = ComputeDailyHoursForClient(clientContracts, d);
                    if (dailyHours > 0)
                        absenzByDate[d] = absenzByDate.GetValueOrDefault(d) + dailyHours * bp.DefaultValue;
                }
            }

            var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
            var dailyData = new List<ResourceMonitorDayResource>(totalDays);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                double maxKapazitaetHours = 0;
                double dienstHours = 0;

                foreach (var cc in contracts)
                {
                    maxKapazitaetHours += ComputeDailyHoursForContract(cc, date);
                }

                foreach (var s in shifts)
                {
                    if (s.FromDate > date || (s.UntilDate.HasValue && s.UntilDate.Value < date)) continue;

                    var isShiftDay = date.DayOfWeek switch
                    {
                        DayOfWeek.Monday    => s.IsMonday,
                        DayOfWeek.Tuesday   => s.IsTuesday,
                        DayOfWeek.Wednesday => s.IsWednesday,
                        DayOfWeek.Thursday  => s.IsThursday,
                        DayOfWeek.Friday    => s.IsFriday,
                        DayOfWeek.Saturday  => s.IsSaturday,
                        DayOfWeek.Sunday    => s.IsSunday,
                        _                   => false,
                    };

                    if (isShiftDay)
                        dienstHours += (double)s.WorkTime;
                }

                absenzByDate.TryGetValue(date, out var absenzHours);

                dailyData.Add(new ResourceMonitorDayResource
                {
                    Date = date,
                    MaxKapazitaetHours = Math.Round(maxKapazitaetHours, 1),
                    DienstHours = Math.Round(dienstHours, 1),
                    AbsenzHours = Math.Round(absenzHours, 1),
                });
            }

            return new ResourceMonitorResource { DailyData = dailyData };
        }, nameof(Handle));
    }

    private static double ComputeDailyHoursForClient(List<ClientContract> clientContracts, DateOnly date)
    {
        double hours = 0;
        foreach (var cc in clientContracts)
        {
            hours += ComputeDailyHoursForContract(cc, date);
        }
        return hours;
    }

    private static double ComputeDailyHoursForContract(ClientContract cc, DateOnly date)
    {
        if (cc.Contract is null) return 0;
        if (cc.FromDate > date || (cc.UntilDate.HasValue && cc.UntilDate.Value < date)) return 0;
        if (!cc.Contract.GuaranteedHours.HasValue) return 0;

        var weeklyH = cc.Contract.PaymentInterval switch
        {
            PaymentInterval.Weekly   => (double)cc.Contract.GuaranteedHours.Value,
            PaymentInterval.Biweekly => (double)cc.Contract.GuaranteedHours.Value / 2.0,
            PaymentInterval.Monthly  => (double)cc.Contract.GuaranteedHours.Value * 12.0 / 52.0,
            _                        => (double)cc.Contract.GuaranteedHours.Value * 12.0 / 52.0,
        };

        var workingDays =
            (cc.Contract.WorkOnMonday    ? 1 : 0) +
            (cc.Contract.WorkOnTuesday   ? 1 : 0) +
            (cc.Contract.WorkOnWednesday ? 1 : 0) +
            (cc.Contract.WorkOnThursday  ? 1 : 0) +
            (cc.Contract.WorkOnFriday    ? 1 : 0) +
            (cc.Contract.WorkOnSaturday  ? 1 : 0) +
            (cc.Contract.WorkOnSunday    ? 1 : 0);

        if (workingDays == 0) return 0;
        if (workingDays == 7) return weeklyH / 7.0;

        var isWorkDay = date.DayOfWeek switch
        {
            DayOfWeek.Monday    => cc.Contract.WorkOnMonday,
            DayOfWeek.Tuesday   => cc.Contract.WorkOnTuesday,
            DayOfWeek.Wednesday => cc.Contract.WorkOnWednesday,
            DayOfWeek.Thursday  => cc.Contract.WorkOnThursday,
            DayOfWeek.Friday    => cc.Contract.WorkOnFriday,
            DayOfWeek.Saturday  => cc.Contract.WorkOnSaturday,
            DayOfWeek.Sunday    => cc.Contract.WorkOnSunday,
            _                   => false,
        };

        return isWorkDay ? weeklyH / workingDays : 0;
    }
}
