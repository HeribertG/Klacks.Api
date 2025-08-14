using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftGroupManagementService
{
    Task UpdateGroupItemsAsync(Guid shiftId, List<Guid> actualGroupIds);

    Task<List<Group>> GetGroupsForShiftAsync(Guid shiftId);
}