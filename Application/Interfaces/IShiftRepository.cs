using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftRepository : IBaseRepository<Shift>
{
    new Task<Shift?> Get(Guid id);

    Task<Shift?> GetTrackedOrFromDb(Guid id);

    IQueryable<Shift> GetQuery();
   
    IQueryable<Shift> GetQueryWithClient();

    IQueryable<Shift> FilterShifts(ShiftFilter filter);

    Task<TruncatedShift> GetFilteredAndPaginatedShifts(ShiftFilter filter);

    Task UpdateGroupItems(Guid shiftId, List<Guid> actualGroupIds);
    
    Task<List<Group>> GetGroupsForShift(Guid shiftId);

    Task<List<Shift>> CutList(Guid id, DateOnly? filterClosedBefore = null, bool tracked = false);

    Task<Shift?> GetSealedOrder(Guid originalId);

    Task<TruncatedShift> GetPaginatedShifts(IQueryable<Shift> filteredQuery, ShiftFilter filter);

    Task<Shift> AddWithSealedOrderHandling(Shift shift);

    Task<Shift?> PutWithSealedOrderHandling(Shift shift);
}
