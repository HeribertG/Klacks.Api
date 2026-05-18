// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for computing per-day capacity, service, and absence hours for the resource monitor dashboard card.
/// DienstHours are derived directly from Shift schedules (FromDate/UntilDate + weekday flags),
/// not from Work entries, to reflect planned capacity rather than recorded actuals.
/// </summary>
/// <param name="context">Database context for ClientContract, Shift, Break, and GroupItem queries</param>
/// <param name="logger">Logger for error handling via BaseHandler</param>
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Enums;
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
                .Where(bp => !bp.IsDeleted
                    && bp.From < periodEnd
                    && bp.Until > periodStart);

            if (groupClientIds != null)
                absenzQuery = absenzQuery.Where(bp => groupClientIds.Contains(bp.ClientId));

            var absenzRanges = await absenzQuery
                .Select(bp => new { bp.From, bp.Until })
                .ToListAsync(cancellationToken);

            var absenzByDate = new Dictionary<DateOnly, double>();
            foreach (var bp in absenzRanges)
            {
                var bpFrom  = bp.From  < periodStart ? periodStart : bp.From;
                var bpUntil = bp.Until > periodEnd   ? periodEnd   : bp.Until;
                var day = DateOnly.FromDateTime(bpFrom);
                var lastDay = DateOnly.FromDateTime(bpUntil);

                for (var d = day; d <= lastDay; d = d.AddDays(1))
                {
                    var dayStart   = d.ToDateTime(TimeOnly.MinValue);
                    var dayEnd     = d.ToDateTime(TimeOnly.MaxValue);
                    var overlapStart = bpFrom  > dayStart ? bpFrom  : dayStart;
                    var overlapEnd   = bpUntil < dayEnd   ? bpUntil : dayEnd;
                    var hours = (overlapEnd - overlapStart).TotalHours;
                    if (hours > 0)
                        absenzByDate[d] = absenzByDate.GetValueOrDefault(d) + hours;
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
                    if (cc.Contract is null) continue;
                    if (cc.FromDate > date || (cc.UntilDate.HasValue && cc.UntilDate.Value < date)) continue;
                    if (!cc.Contract.GuaranteedHours.HasValue) continue;

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

                    if (workingDays == 0) continue;

                    var isContractDay = date.DayOfWeek switch
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

                    if (workingDays == 7 || isContractDay)
                        maxKapazitaetHours += workingDays == 7 ? weeklyH / 7.0 : weeklyH / workingDays;
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
}
