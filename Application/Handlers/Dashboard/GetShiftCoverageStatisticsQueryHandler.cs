// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving shift coverage and sealing statistics per group.
/// </summary>
/// <param name="context">Database context for GroupItem, Group and Work queries</param>
/// <param name="shiftScheduleService">Service for shift schedule queries</param>
/// <param name="groupFilterService">Service for determining the user's visible groups</param>
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetShiftCoverageStatisticsQueryHandler : BaseHandler, IRequestHandler<GetShiftCoverageStatisticsQuery, IEnumerable<ShiftCoverageStatisticsResource>>
{
    private readonly DataBaseContext _context;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IShiftGroupFilterService _groupFilterService;

    public GetShiftCoverageStatisticsQueryHandler(
        DataBaseContext context,
        IShiftScheduleService shiftScheduleService,
        IShiftGroupFilterService groupFilterService,
        ILogger<GetShiftCoverageStatisticsQueryHandler> logger)
        : base(logger)
    {
        _context = context;
        _shiftScheduleService = shiftScheduleService;
        _groupFilterService = groupFilterService;
    }

    public async Task<IEnumerable<ShiftCoverageStatisticsResource>> Handle(GetShiftCoverageStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var startDate = new DateOnly(today.Year, today.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var visibleGroupIds = await _groupFilterService.GetVisibleGroupIdsAsync(null);
            var hasGroupFilter = visibleGroupIds.Count > 0;

            var shiftAssignments = await _shiftScheduleService
                .GetShiftScheduleQuery(startDate, endDate, visibleGroupIds: hasGroupFilter ? visibleGroupIds : null)
                .ToListAsync(cancellationToken);

            var groupItems = await _context.GroupItem
                .Where(gi => gi.ShiftId != null && !gi.IsDeleted)
                .Select(gi => new { gi.ShiftId, gi.GroupId })
                .ToListAsync(cancellationToken);

            var groups = await _context.Group
                .Where(g => !g.IsDeleted)
                .Select(g => new { g.Id, g.Name })
                .ToListAsync(cancellationToken);

            var workEntries = await _context.Work
                .Where(w => !w.IsDeleted && w.CurrentDate >= startDate && w.CurrentDate <= endDate)
                .Select(w => new { w.ShiftId, w.LockLevel })
                .ToListAsync(cancellationToken);

            var shiftToGroups = groupItems
                .Where(gi => gi.ShiftId.HasValue)
                .GroupBy(gi => gi.ShiftId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.GroupId).Distinct().ToList());

            var groupNameMap = groups.ToDictionary(g => g.Id, g => g.Name);

            var coverageByGroup = new Dictionary<Guid, (int TotalSlots, int CoveredSlots)>();
            var workByGroup = new Dictionary<Guid, (int Total, int Sealed)>();

            foreach (var assignment in shiftAssignments)
            {
                if (!shiftToGroups.TryGetValue(assignment.ShiftId, out var assignedGroupIds))
                {
                    continue;
                }

                foreach (var groupId in assignedGroupIds)
                {
                    if (hasGroupFilter && !visibleGroupIds.Contains(groupId))
                    {
                        continue;
                    }

                    if (!coverageByGroup.ContainsKey(groupId))
                    {
                        coverageByGroup[groupId] = (0, 0);
                    }

                    var current = coverageByGroup[groupId];
                    coverageByGroup[groupId] = (current.TotalSlots + assignment.Quantity, current.CoveredSlots + assignment.Engaged);
                }
            }

            foreach (var work in workEntries)
            {
                if (!shiftToGroups.TryGetValue(work.ShiftId, out var assignedGroupIds))
                {
                    continue;
                }

                foreach (var groupId in assignedGroupIds)
                {
                    if (hasGroupFilter && !visibleGroupIds.Contains(groupId))
                    {
                        continue;
                    }

                    if (!workByGroup.ContainsKey(groupId))
                    {
                        workByGroup[groupId] = (0, 0);
                    }

                    var current = workByGroup[groupId];
                    var isSealed = work.LockLevel >= WorkLockLevel.Confirmed ? 1 : 0;
                    workByGroup[groupId] = (current.Total + 1, current.Sealed + isSealed);
                }
            }

            var allGroupIds = coverageByGroup.Keys.Union(workByGroup.Keys).Distinct();

            return allGroupIds
                .Where(gid => groupNameMap.ContainsKey(gid))
                .Select(gid =>
                {
                    var coverage = coverageByGroup.GetValueOrDefault(gid, (0, 0));
                    var work = workByGroup.GetValueOrDefault(gid, (0, 0));

                    return new ShiftCoverageStatisticsResource
                    {
                        GroupId = gid,
                        GroupName = groupNameMap[gid],
                        TotalSlots = coverage.Item1,
                        CoveredSlots = coverage.Item2,
                        TotalWorkEntries = work.Item1,
                        SealedWorkEntries = work.Item2
                    };
                })
                .Where(r => r.TotalSlots > 0 || r.TotalWorkEntries > 0)
                .OrderBy(r => r.GroupName)
                .ToList();
        }, nameof(Handle));
    }
}
