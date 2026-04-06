// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ContainerLockRepository : IContainerLockRepository
{
    private readonly DataBaseContext _context;

    public ContainerLockRepository(DataBaseContext context)
    {
        _context = context;
    }

    public Task<ContainerLock?> GetByResource(string resourceType, Guid resourceId, CancellationToken cancellationToken)
    {
        return _context.ContainerLock
            .FirstOrDefaultAsync(l => l.ResourceType == resourceType && l.ResourceId == resourceId, cancellationToken);
    }

    public Task<ContainerLock?> GetById(Guid id, CancellationToken cancellationToken)
    {
        return _context.ContainerLock.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<ContainerLock> Add(ContainerLock containerLock, CancellationToken cancellationToken)
    {
        await _context.ContainerLock.AddAsync(containerLock, cancellationToken);
        return containerLock;
    }

    public Task Update(ContainerLock containerLock, CancellationToken cancellationToken)
    {
        _context.ContainerLock.Update(containerLock);
        return Task.CompletedTask;
    }

    public Task Delete(ContainerLock containerLock, CancellationToken cancellationToken)
    {
        _context.ContainerLock.Remove(containerLock);
        return Task.CompletedTask;
    }

    public async Task DeleteStale(string resourceType, Guid resourceId, DateTime threshold, CancellationToken cancellationToken)
    {
        var stale = await _context.ContainerLock
            .Where(l => l.ResourceType == resourceType && l.ResourceId == resourceId && l.LastHeartbeatAt < threshold)
            .ToListAsync(cancellationToken);

        if (stale.Count > 0)
        {
            _context.ContainerLock.RemoveRange(stale);
        }
    }

    public async Task<bool> IsHeldBy(string resourceType, Guid resourceId, Guid userId, string instanceId, CancellationToken cancellationToken)
    {
        var existing = await _context.ContainerLock
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ResourceType == resourceType && l.ResourceId == resourceId, cancellationToken);

        return existing != null && existing.UserId == userId && existing.InstanceId == instanceId;
    }
}
