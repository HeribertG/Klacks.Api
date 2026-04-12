// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for ContainerShiftOverride entities with date-based lookups and work existence checks.
/// </summary>
/// <param name="containerId">The container shift ID to query overrides for</param>
/// <param name="date">The specific date to look up</param>
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ContainerShiftOverrideRepository : IContainerShiftOverrideRepository
{
    private readonly DataBaseContext _context;

    public ContainerShiftOverrideRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<ContainerShiftOverride?> GetByContainerAndDate(Guid containerId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.ContainerShiftOverrides
            .FirstOrDefaultAsync(o => o.ContainerId == containerId && o.Date == date, ct);
    }

    public async Task<ContainerShiftOverride?> GetByContainerAndDateWithItems(Guid containerId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.ContainerShiftOverrides
            .Include(o => o.ContainerShiftOverrideItems.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Shift)
            .Include(o => o.ContainerShiftOverrideItems.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Absence)
            .Include(o => o.Shift)
            .FirstOrDefaultAsync(o => o.ContainerId == containerId && o.Date == date, ct);
    }

    public async Task<List<ContainerShiftOverride>> GetByContainerAndDateRange(Guid containerId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        return await _context.ContainerShiftOverrides
            .Where(o => o.ContainerId == containerId && o.Date >= fromDate && o.Date <= toDate)
            .ToListAsync(ct);
    }

    public async Task<ContainerShiftOverride?> GetWithTracking(Guid overrideId, CancellationToken ct = default)
    {
        return await _context.ContainerShiftOverrides
            .Include(o => o.ContainerShiftOverrideItems)
            .FirstOrDefaultAsync(o => o.Id == overrideId, ct);
    }

    public async Task Add(ContainerShiftOverride entity)
    {
        await _context.ContainerShiftOverrides.AddAsync(entity);
    }

    public async Task Delete(ContainerShiftOverride entity)
    {
        _context.ContainerShiftOverrides.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task<bool> HasWorkForOverride(Guid containerId, DateOnly date, CancellationToken ct = default)
    {
        return await _context.Work
            .AnyAsync(w => w.ShiftId == containerId
                && w.CurrentDate == date
                && !w.ParentWorkId.HasValue, ct);
    }
}
