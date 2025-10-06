using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Interfaces;

public interface IGroupItemRepository : IBaseRepository<GroupItem>
{
    Task<GroupItem?> GetByClientAndGroup(Guid clientId, Guid groupId);
}
