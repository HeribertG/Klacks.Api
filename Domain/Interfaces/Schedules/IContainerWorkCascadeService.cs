// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Cascades operations (delete, restore, move, lock-level) from a container work to its children.
/// </summary>
/// <param name="parentWorkId">The ID of the container work whose children should be affected</param>
/// <param name="deletedTime">Delete stamp of the container; only children deleted within the sibling tolerance around it are restored</param>
/// <param name="deletedBy">User stamped on the container's delete; only children carrying the same user are restored</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerWorkCascadeService
{
    Task DeleteChildrenAsync(Guid parentWorkId);

    Task RestoreChildrenAsync(Guid parentWorkId, DateTime deletedTime, string? deletedBy);

    Task MoveChildrenAsync(Guid parentWorkId, DateOnly newDate, Guid newClientId);

    Task UpdateLockLevelAsync(Guid parentWorkId, WorkLockLevel lockLevel, string? sealedBy);
}
