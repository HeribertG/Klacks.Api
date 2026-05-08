// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query and handler for computing per-day Soll and Ist hours for the resource monitor dashboard card.
/// </summary>
/// <param name="Year">Calendar year to compute (e.g. 2026)</param>
/// <param name="GroupId">Optional group filter — null means all employees</param>
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Queries.Dashboard;

public record GetResourceMonitorQuery(int Year, Guid? GroupId) : IRequest<ResourceMonitorResource>;

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
                    && cc.FromDate <= endDate
                    && (cc.UntilDate == null || cc.UntilDate >= startDate));

            if (groupClientIds != null)
                contractQuery = contractQuery.Where(cc => groupClientIds.Contains(cc.ClientId));

            var contracts = await contractQuery.ToListAsync(cancellationToken);

            var worksQuery = _context.Work
                .Where(w => !w.IsDeleted
                    && w.CurrentDate >= startDate
                    && w.CurrentDate <= endDate
                    && w.AnalyseToken == null);

            if (groupShiftIds != null)
                worksQuery = worksQuery.Where(w => groupShiftIds.Contains(w.ShiftId));

            var actualByDate = await worksQuery
                .GroupBy(w => w.CurrentDate)
                .Select(g => new { Date = g.Key, Hours = g.Sum(w => w.WorkTime) })
                .ToDictionaryAsync(x => x.Date, x => (double)x.Hours, cancellationToken);

            var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
            var dailyData = new List<ResourceMonitorDayResource>(totalDays);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                double shouldHours = 0;

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

                    if (workingDays == 7)
                    {
                        shouldHours += weeklyH / 7.0;
                    }
                    else
                    {
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
                        if (isWorkDay) shouldHours += weeklyH / workingDays;
                    }
                }

                actualByDate.TryGetValue(date, out var actualHours);

                dailyData.Add(new ResourceMonitorDayResource
                {
                    Date = date,
                    ShouldHours = Math.Round(shouldHours, 1),
                    ActualHours = Math.Round(actualHours, 1),
                });
            }

            return new ResourceMonitorResource { DailyData = dailyData };
        }, nameof(Handle));
    }
}
