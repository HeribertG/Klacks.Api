using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public class ShiftScheduleTypeFilterService : IShiftScheduleTypeFilterService
{
    public IQueryable<ShiftDayAssignment> ApplyTypeFilter(
        IQueryable<ShiftDayAssignment> query,
        bool? isSporadic,
        bool? isTimeRange,
        bool? container,
        bool? isStandartShift)
    {
        if (isSporadic.HasValue)
        {
            query = query.Where(s => s.IsSporadic == isSporadic.Value);
        }

        if (isTimeRange.HasValue)
        {
            query = query.Where(s => s.IsTimeRange == isTimeRange.Value);
        }

        // TODO: container und isStandartShift Filter aktivieren wenn SQL-Funktion angepasst
        // if (container.HasValue)
        // {
        //     query = query.Where(s => s.Container == container.Value);
        // }
        //
        // if (isStandartShift.HasValue)
        // {
        //     query = query.Where(s => s.IsStandartShift == isStandartShift.Value);
        // }

        return query;
    }
}
