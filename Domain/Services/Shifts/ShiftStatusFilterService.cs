using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftStatusFilterService : IShiftStatusFilterService
{
    public IQueryable<Shift> ApplyStatusFilter(IQueryable<Shift> query, bool isOriginal)
    {
        if (isOriginal)
        {
            return query.Where(shift => shift.Status == ShiftStatus.Original);
        }
        else
        {
            return query.Where(shift => shift.Status >= ShiftStatus.IsCutOriginal);
        }
    }
}