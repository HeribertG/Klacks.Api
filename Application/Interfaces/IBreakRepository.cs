using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IBreakRepository : IBaseRepository<Break>
{
    Task<(Break Break, PeriodHoursResource PeriodHours)> AddWithPeriodHours(Break breakEntry, DateOnly periodStart, DateOnly periodEnd);
    Task<(Break? Break, PeriodHoursResource? PeriodHours)> PutWithPeriodHours(Break breakEntry, DateOnly periodStart, DateOnly periodEnd);
    Task<(Break? Break, PeriodHoursResource? PeriodHours)> DeleteWithPeriodHours(Guid id, DateOnly periodStart, DateOnly periodEnd);
}
