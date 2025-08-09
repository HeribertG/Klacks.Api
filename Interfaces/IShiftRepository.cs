using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Presentation.Resources.Filter;

namespace Klacks.Api.Interfaces;

public interface IShiftRepository : IBaseRepository<Shift>
{
    new Task<Shift?> Get(Guid id);
    
    IQueryable<Shift> GetQuery();
   
    IQueryable<Shift> GetQueryWithClient();

    IQueryable<Shift> FilterShifts(ShiftFilter filter);

    Task<TruncatedShift> GetFilteredAndPaginatedShifts(ShiftFilter filter);

    Task UpdateGroupItems(Guid shiftId, List<Guid> actualGroupIds);
    
    Task<List<Group>> GetGroupsForShift(Guid shiftId);

    Task<List<Shift>> CutList(Guid id);

    Task<TruncatedShift> GetPaginatedShifts(IQueryable<Shift> filteredQuery, ShiftFilter filter);
}
