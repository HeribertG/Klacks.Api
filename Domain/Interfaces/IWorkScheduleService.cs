using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IWorkScheduleService
{
    IQueryable<WorkScheduleEntry> GetWorkScheduleQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<Guid>? visibleGroupIds = null);
}
