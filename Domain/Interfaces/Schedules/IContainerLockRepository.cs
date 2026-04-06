// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerLockRepository
{
    Task<ContainerLock?> GetByResource(string resourceType, Guid resourceId, CancellationToken cancellationToken);

    Task<ContainerLock?> GetById(Guid id, CancellationToken cancellationToken);

    Task<ContainerLock> Add(ContainerLock containerLock, CancellationToken cancellationToken);

    Task Update(ContainerLock containerLock, CancellationToken cancellationToken);

    Task Delete(ContainerLock containerLock, CancellationToken cancellationToken);

    Task DeleteStale(string resourceType, Guid resourceId, DateTime threshold, CancellationToken cancellationToken);

    Task<bool> IsHeldBy(string resourceType, Guid resourceId, Guid userId, string instanceId, CancellationToken cancellationToken);
}
