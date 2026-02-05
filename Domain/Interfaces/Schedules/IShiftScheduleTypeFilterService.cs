using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftScheduleTypeFilterService
{
    IQueryable<ShiftDayAssignment> ApplyTypeFilter(
        IQueryable<ShiftDayAssignment> query,
        bool isSporadic,
        bool isTimeRange,
        bool container,
        bool isStandartShift);
}
