using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources.Filter;

namespace Klacks.Api.Interfaces.Domains;

public interface IShiftFilterService
{
    IQueryable<Shift> ApplyAllFilters(IQueryable<Shift> query, ShiftFilter filter);
    
    Task<TruncatedShift> GetFilteredAndPaginatedShifts(ShiftFilter filter);
}