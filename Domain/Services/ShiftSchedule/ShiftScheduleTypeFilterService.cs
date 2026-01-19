using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public class ShiftScheduleTypeFilterService : IShiftScheduleTypeFilterService
{
    public IQueryable<ShiftDayAssignment> ApplyTypeFilter(
        IQueryable<ShiftDayAssignment> query,
        bool isSporadic,
        bool isTimeRange,
        bool container,
        bool isStandartShift)
    {
        if (!isSporadic && !isTimeRange && !container && !isStandartShift)
        {
            return query.Where(s => false);
        }

        return query.Where(s =>
            (isSporadic && s.IsSporadic) ||
            (isTimeRange && s.IsTimeRange) ||
            (container && s.ShiftType == (int)ShiftType.IsContainer) ||
            (isStandartShift &&
             !s.IsSporadic &&
             !s.IsTimeRange &&
             s.ShiftType == (int)ShiftType.IsTask &&
             (s.Status == (int)ShiftStatus.OriginalShift || s.Status == (int)ShiftStatus.SplitShift)));
    }
}
