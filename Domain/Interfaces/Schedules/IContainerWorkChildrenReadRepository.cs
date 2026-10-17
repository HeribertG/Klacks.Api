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

    Task<ContainerTemplate?> GetContainerTemplate(Guid containerId, int weekday, bool isHoliday, CancellationToken cancellationToken);
}
