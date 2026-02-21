using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkRepository : IBaseRepository<Work>
{
    Task<(List<Client> Clients, int TotalCount)> WorkList(WorkFilter filter);
    Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursForClients(List<Guid> clientIds, DateOnly startDate, DateOnly endDate);
    Task<List<Work>> GetByClientAndDateRangeAsync(Guid clientId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default);
}
