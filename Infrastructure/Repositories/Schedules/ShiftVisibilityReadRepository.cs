// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core reads for the shift write guard. The visibility query applies the shared ShiftGroupVisibilityFilter
/// rule, so the write check matches the shift reads exactly.
/// </summary>
/// <param name="context">EF Core context holding Shift, GroupItem and the container template and override items</param>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ShiftVisibilityReadRepository : IShiftVisibilityReader
{
    private readonly DataBaseContext _context;

    public ShiftVisibilityReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> FilterVisibleShiftIdsAsync(
        IReadOnlyCollection<Guid> shiftIds, IReadOnlyCollection<Guid> visibleGroupIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = shiftIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return [];
        }

        var query = _context.Shift.AsNoTracking().Where(s => distinctIds.Contains(s.Id));
        return await ShiftGroupVisibilityFilter.Apply(query, visibleGroupIds)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<GroupItem>> GetActiveGroupItemsAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await _context.GroupItem
            .AsNoTracking()
            .Where(gi => gi.ShiftId == shiftId && !gi.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await _context.Shift.AsNoTracking().AnyAsync(s => s.Id == shiftId, cancellationToken);
    }

    public async Task<List<Guid>> GetContainerItemShiftIdsAsync(Guid containerId, CancellationToken cancellationToken = default)
    {
        var templateShiftIds = _context.ContainerTemplateItem
            .Where(i => i.ContainerTemplate.ContainerId == containerId && !i.ContainerTemplate.IsDeleted && i.ShiftId != null)
            .Select(i => i.ShiftId!.Value);
        var overrideShiftIds = _context.ContainerShiftOverrideItems
            .Where(i => i.ContainerShiftOverride.ContainerId == containerId && !i.ContainerShiftOverride.IsDeleted && i.ShiftId != null)
            .Select(i => i.ShiftId!.Value);

        return await templateShiftIds.Union(overrideShiftIds).ToListAsync(cancellationToken);
    }
}
