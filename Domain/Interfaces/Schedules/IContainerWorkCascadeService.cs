// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Cascades operations (delete, restore, move, lock-level) from a container work to its children.
/// </summary>
/// <param name="parentWorkId">The ID of the container work whose children should be affected</param>
/// <param name="deletedTime">Delete stamp of the container; only children deleted within the sibling tolerance around it are restored</param>
/// <param name="deletedBy">User stamped on the container's delete; only children carrying the same user are restored</param>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerWorkCascadeService
{
    Task DeleteChildrenAsync(Guid parentWorkId);

    /// <summary>
    /// Reads, without changing anything, the live child Works and Breaks of the given container Works: the very entries
    /// DeleteChildrenAsync deletes for each of them.
    /// </summary>
    Task<ContainerChildIds> GetChildIdsAsync(
        IReadOnlyCollection<Guid> parentWorkIds, CancellationToken cancellationToken = default);

    Task RestoreChildrenAsync(Guid parentWorkId, DateTime deletedTime, string? deletedBy);

    Task MoveChildrenAsync(Guid parentWorkId, DateOnly newDate, Guid newClientId);

    Task UpdateLockLevelAsync(Guid parentWorkId, WorkLockLevel lockLevel, string? sealedBy);
}
