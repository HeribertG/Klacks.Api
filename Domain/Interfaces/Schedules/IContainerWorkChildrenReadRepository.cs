// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerWorkChildrenReadRepository
{
    Task<Work?> GetParentWorkNoTracking(Guid workId, CancellationToken cancellationToken);

    Task<List<Work>> GetChildWorksWithShiftClient(Guid parentWorkId, CancellationToken cancellationToken);

    /// <summary>
    /// The stored shift id of every not deleted sub-work of the container work, keyed by sub-work id, untracked.
    /// </summary>
    /// <param name="parentWorkId">Container work whose sub-works are read</param>
    Task<IReadOnlyDictionary<Guid, Guid>> GetChildWorkShiftIds(Guid parentWorkId, CancellationToken cancellationToken);

    Task<List<Break>> GetChildBreaksWithAbsence(Guid parentWorkId, CancellationToken cancellationToken);

    Task<List<WorkChange>> GetWorkChangesForWorks(IReadOnlyCollection<Guid> workIds, CancellationToken cancellationToken);

    /// <summary>
    /// Which of the given ids are taken in the work, break and work change tables, soft-deleted rows included, untracked.
    /// A new child may only take an id nobody has taken.
    /// </summary>
    /// <param name="workIds">Ids a request wants to give new sub-works</param>
    /// <param name="breakIds">Ids a request wants to give new sub-breaks</param>
    /// <param name="workChangeIds">Ids a request wants to give new work changes</param>
    Task<UsedEntryIds> GetUsedEntryIds(
        IReadOnlyCollection<Guid> workIds,
        IReadOnlyCollection<Guid> breakIds,
        IReadOnlyCollection<Guid> workChangeIds,
        CancellationToken cancellationToken);

    Task<ContainerTemplate?> GetContainerTemplate(Guid containerId, int weekday, bool isHoliday, CancellationToken cancellationToken);
}
