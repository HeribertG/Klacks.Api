// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftCoverageReadRepository
{
    Task<List<(Guid? ShiftId, Guid GroupId)>> GetShiftGroupAssignments(CancellationToken cancellationToken);

    Task<List<(Guid Id, string Name)>> GetActiveGroups(CancellationToken cancellationToken);

    Task<List<(Guid ShiftId, WorkLockLevel LockLevel)>> GetWorkLockEntries(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);
}
