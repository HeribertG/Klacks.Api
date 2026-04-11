// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IBreakRepository : IBaseRepository<Break>
{
    Task<List<Break>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);

    Task<List<(DateOnly Date, int Total, int Sealed)>> GetSealingSummaryAsync(DateOnly from, DateOnly to, Guid? groupId, CancellationToken cancellationToken = default);
}
