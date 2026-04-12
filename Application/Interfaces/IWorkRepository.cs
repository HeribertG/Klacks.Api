// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkRepository : IBaseRepository<Work>
{
    Task<List<Work>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<(List<Client> Clients, int TotalCount)> WorkList(WorkFilter filter, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursForClients(List<Guid> clientIds, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<List<Work>> GetByClientAndDateRangeAsync(Guid clientId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);

    Task<List<(DateOnly Date, int Total, int Sealed)>> GetSealingSummaryAsync(DateOnly from, DateOnly to, Guid? groupId, CancellationToken cancellationToken = default);

    Task<List<UsedPeriodDto>> GetUsedPeriodsAsync(CancellationToken cancellationToken = default);
}
