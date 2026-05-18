// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for computing per-day employee headcount for the resource monitor dashboard card.
/// MaxKapazitaet  = number of active contracts whose employee works this weekday.
/// Dienste        = number of shifts scheduled on this date.
/// Absenzen       = sum of Absence.DefaultValue for employees absent on this date (1.0 = full, 0.5 = half),
///                  counted only on weekdays the employee actually works.
/// </summary>
/// <param name="context">Database context for ClientContract, Shift, BreakPlaceholder, and GroupItem queries</param>
/// <param name="logger">Logger for error handling via BaseHandler</param>
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
                    if (!ClientWorksOnDay(clientContracts, d)) continue;
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
                    if (ClientWorksOnDay(clientContracts, date))
                        maxKapaCount += 1;
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
                    MaxKapazitaetCount = maxKapaCount,
                    DienstCount = dienstCount,
                    AbsenzCount = Math.Round(absenzCount, 2),
                });
            }

            return new ResourceMonitorResource { DailyData = dailyData };
        }, nameof(Handle));
    }

    private static bool ClientWorksOnDay(List<ClientContract> clientContracts, DateOnly date)
    {
        foreach (var cc in clientContracts)
        {
            if (cc.Contract is null) continue;
            if (cc.FromDate > date || (cc.UntilDate.HasValue && cc.UntilDate.Value < date)) continue;
            if (IsWeekdayActive(date,
                cc.Contract.WorkOnMonday, cc.Contract.WorkOnTuesday, cc.Contract.WorkOnWednesday,
                cc.Contract.WorkOnThursday, cc.Contract.WorkOnFriday,
                cc.Contract.WorkOnSaturday, cc.Contract.WorkOnSunday))
            {
                return true;
            }
        }
        return false;
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
