using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IBreakRepository : IBaseRepository<Break>
{
    Task<(Break Break, PeriodHoursResource PeriodHours)> AddWithPeriodHours(Break breakEntry, DateOnly periodStart, DateOnly periodEnd);
    Task<(Break? Break, PeriodHoursResource? PeriodHours)> PutWithPeriodHours(Break breakEntry, DateOnly periodStart, DateOnly periodEnd);
    Task<(Break? Break, PeriodHoursResource? PeriodHours)> DeleteWithPeriodHours(Guid id, DateOnly periodStart, DateOnly periodEnd);
    Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default);
}
