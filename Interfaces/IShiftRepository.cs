using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources.Filter;

namespace Klacks.Api.Interfaces;

public interface IShiftRepository : IBaseRepository<Shift>
{
    Task<TruncatedShift> Truncated(ShiftFilter filter);

    Task UpdateGroupItems(Guid shiftId, List<Guid> actualGroupIds);
}
