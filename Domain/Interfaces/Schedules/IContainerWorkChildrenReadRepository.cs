// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerWorkChildrenReadRepository
{
    Task<Work?> GetParentWorkNoTracking(Guid workId, CancellationToken cancellationToken);

    Task<List<Work>> GetChildWorksWithShiftClient(Guid parentWorkId, CancellationToken cancellationToken);

    Task<List<Break>> GetChildBreaksWithAbsence(Guid parentWorkId, CancellationToken cancellationToken);

    Task<List<WorkChange>> GetWorkChangesForWorks(IReadOnlyCollection<Guid> workIds, CancellationToken cancellationToken);

    Task<ContainerTemplate?> GetContainerTemplate(Guid containerId, int weekday, bool isHoliday, CancellationToken cancellationToken);
}
