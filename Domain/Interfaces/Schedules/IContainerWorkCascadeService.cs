// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Cascades operations (delete, move, lock-level) from a container work to its children.
/// </summary>
/// <param name="parentWorkId">The ID of the container work whose children should be affected</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerWorkCascadeService
{
    Task DeleteChildrenAsync(Guid parentWorkId);

    Task MoveChildrenAsync(Guid parentWorkId, DateOnly newDate);

    Task UpdateLockLevelAsync(Guid parentWorkId, WorkLockLevel lockLevel, string? sealedBy);
}
