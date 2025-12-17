using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftScheduleService
{
    IQueryable<ShiftDayAssignment> GetShiftScheduleQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<DateOnly>? holidayDates = null,
        List<Guid>? visibleGroupIds = null);
}
