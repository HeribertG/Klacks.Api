// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads shift-to-group memberships from GroupItem for proactive-audience scoping. Scenario
/// memberships (AnalyseToken or ScenarioSourceGroupItemId set) are excluded so a what-if scenario can
/// never move a real notification audience, and soft-deleted rows are excluded explicitly because
/// GroupItem carries no global query filter. GroupItem.ValidFrom / ValidUntil are deliberately NOT
/// evaluated: a membership row is taken as the shift's current audience scope for as long as it
/// exists, which is how GetShiftCoverageStatisticsQueryHandler reads the same join.
///
/// Both queries filter and project on the entity itself and never filter on an already-projected
/// shape. That is not style: PostgreSQL rejected the projected form ("The LINQ expression ... could
/// not be translated"), while the EF InMemory provider the unit tests use evaluates it client-side and
/// passes - so the shape below is the only one proven against the real database.
/// </summary>
/// <param name="context">EF Core context holding GroupItem and Work.</param>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ShiftGroupScopeReadRepository : IShiftGroupScopeReader
{
    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> Empty =
        new Dictionary<Guid, IReadOnlyList<Guid>>();

    private readonly DataBaseContext _context;

    public ShiftGroupScopeReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetGroupIdsByShiftIdsAsync(
        IReadOnlyCollection<Guid> shiftIds,
        CancellationToken cancellationToken = default)
    {
        if (shiftIds.Count == 0)
        {
            return Empty;
        }

        var distinctShiftIds = shiftIds.Distinct().Select(shiftId => (Guid?)shiftId).ToList();

        var rows = await _context.GroupItem
            .Where(groupItem => groupItem.ShiftId != null
                && !groupItem.IsDeleted
                && groupItem.AnalyseToken == null
                && groupItem.ScenarioSourceGroupItemId == null
                && distinctShiftIds.Contains(groupItem.ShiftId))
            .Select(groupItem => new { ShiftId = groupItem.ShiftId!.Value, groupItem.GroupId })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.ShiftId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)group
                    .Select(row => row.GroupId)
                    .Distinct()
                    .OrderBy(groupId => groupId)
                    .ToList());
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetGroupIdsByWorkIdsAsync(
        IReadOnlyCollection<Guid> workIds,
        CancellationToken cancellationToken = default)
    {
        if (workIds.Count == 0)
        {
            return Empty;
        }

        var distinctWorkIds = workIds.Distinct().ToList();

        var workShifts = await _context.Work
            .Where(work => distinctWorkIds.Contains(work.Id) && !work.IsDeleted)
            .Select(work => new { WorkId = work.Id, work.ShiftId })
            .ToListAsync(cancellationToken);

        if (workShifts.Count == 0)
        {
            return Empty;
        }

        var groupsByShift = await GetGroupIdsByShiftIdsAsync(
            workShifts.Select(row => row.ShiftId).Distinct().ToList(), cancellationToken);

        var groupsByWork = new Dictionary<Guid, IReadOnlyList<Guid>>();
        foreach (var row in workShifts)
        {
            if (groupsByShift.TryGetValue(row.ShiftId, out var groupIds))
            {
                groupsByWork[row.WorkId] = groupIds;
            }
        }

        return groupsByWork;
    }
}
