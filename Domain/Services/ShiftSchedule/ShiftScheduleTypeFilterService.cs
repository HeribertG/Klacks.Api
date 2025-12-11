using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public class ShiftScheduleTypeFilterService : IShiftScheduleTypeFilterService
{
    public IQueryable<ShiftDayAssignment> ApplyTypeFilter(
        IQueryable<ShiftDayAssignment> query,
        int? shiftType,
        bool? isSporadic,
        bool? isTimeRange)
    {
        if (shiftType.HasValue)
        {
            query = query.Where(s => s.ShiftType == shiftType.Value);
        }

        if (isSporadic.HasValue)
        {
            query = query.Where(s => s.IsSporadic == isSporadic.Value);
        }

        if (isTimeRange.HasValue)
        {
            query = query.Where(s => s.IsTimeRange == isTimeRange.Value);
        }

        return query;
    }
}
