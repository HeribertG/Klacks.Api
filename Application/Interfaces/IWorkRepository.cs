using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkRepository : IBaseRepository<Work>
{
    Task<(List<Client> Clients, int TotalCount)> WorkList(WorkFilter filter);
    Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursForClients(List<Guid> clientIds, DateOnly startDate, DateOnly endDate);
    Task<(Work Work, PeriodHoursResource PeriodHours)> AddWithPeriodHours(Work work, DateOnly periodStart, DateOnly periodEnd);
    Task<(Work? Work, PeriodHoursResource? PeriodHours)> PutWithPeriodHours(Work work, DateOnly periodStart, DateOnly periodEnd);
    Task<(Work? Work, PeriodHoursResource? PeriodHours)> DeleteWithPeriodHours(Guid id, DateOnly periodStart, DateOnly periodEnd);
}
