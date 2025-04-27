using Klacks.Api.Models.Associations;
using Klacks.Api.Resources.Filter;

namespace Klacks.Api.Interfaces;

public interface IGroupRepository : IBaseRepository<Group>
{
    new Task<Group?> Get(Guid id);
    Task<TruncatedGroup> Truncated(GroupFilter filter);
    Task<Group> AddChildNode(Guid parentId, Group newGroup);
    Task<Group> AddRootNode(Group newGroup);
    Task DeleteNode(Guid id);
    Task MoveNode(Guid nodeId, Guid newParentId);
    Task<IEnumerable<Group>> GetChildren(Guid parentId);
    Task<IEnumerable<Group>> GetTree(Guid? rootId = null);
    Task<IEnumerable<Group>> GetPath(Guid nodeId);
    Task UpdateNode(Group updatedGroup);
    Task<int> GetNodeDepth(Guid nodeId);
}
