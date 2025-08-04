using Klacks.Api.Enums;
using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Services.Shifts;

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