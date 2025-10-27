using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftStatusFilterService : IShiftStatusFilterService
{
    public IQueryable<Shift> ApplyStatusFilter(IQueryable<Shift> query, ShiftFilterType filterType, bool isSealedOrder = false)
    {
        return filterType switch
        {
            ShiftFilterType.Original => isSealedOrder
                ? query.Where(shift => shift.Status == ShiftStatus.OriginalOrder || shift.Status == ShiftStatus.SealedOrder)
                : query.Where(shift => shift.Status == ShiftStatus.OriginalOrder),
            ShiftFilterType.Shift => query.Where(shift => shift.Status >= ShiftStatus.OriginalShift && !shift.IsContainer),
            ShiftFilterType.Container => query.Where(shift => shift.IsContainer),
            ShiftFilterType.Absence => query.Where(shift => false),
            _ => query
        };
    }
}