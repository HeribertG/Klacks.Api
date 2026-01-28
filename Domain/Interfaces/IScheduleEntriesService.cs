using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IScheduleEntriesService
{
    IQueryable<ScheduleCell> GetScheduleEntriesQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<Guid>? visibleGroupIds = null);
}
