// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-side repository for loading the children (sub-works, sub-breaks, work changes) of a container work.
/// </summary>
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ContainerWorkChildrenReadRepository : IContainerWorkChildrenReadRepository
{
    private readonly DataBaseContext _context;

    public ContainerWorkChildrenReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public Task<Work?> GetParentWorkNoTracking(Guid workId, CancellationToken cancellationToken)
    {
        return _context.Work
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workId, cancellationToken);
    }

    public Task<List<Work>> GetChildWorksWithShiftClient(Guid parentWorkId, CancellationToken cancellationToken)
    {
        return _context.Work
            .Include(w => w.Shift)
                .ThenInclude(s => s!.Client)
                    .ThenInclude(c => c!.Addresses)
            .Where(w => w.ParentWorkId == parentWorkId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<Break>> GetChildBreaksWithAbsence(Guid parentWorkId, CancellationToken cancellationToken)
    {
        return _context.Break
            .Include(b => b.Absence)
            .Where(b => b.ParentWorkId == parentWorkId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkChange>> GetWorkChangesForWorks(IReadOnlyCollection<Guid> workIds, CancellationToken cancellationToken)
    {
        if (workIds.Count == 0)
        {
            return new List<WorkChange>();
        }

        return await _context.WorkChange
            .Where(wc => workIds.Contains(wc.WorkId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<ContainerTemplate?> GetContainerTemplate(Guid containerId, int weekday, bool isHoliday, CancellationToken cancellationToken)
    {
        return _context.ContainerTemplate
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.ContainerId == containerId
                    && t.Weekday == weekday
                    && t.IsHoliday == isHoliday,
                cancellationToken);
    }
}
