using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface IGroupRepository : IBaseRepository<Group>
{
    new Task<Group?> Get(Guid id);

    Task<TruncatedGroup> Truncated(GroupFilter filter);

    Task MoveNode(Guid nodeId, Guid newParentId);

    Task<IEnumerable<Group>> GetChildren(Guid parentId);

    Task<IEnumerable<Group>> GetTree(Guid? rootId = null);

    Task<IEnumerable<Group>> GetPath(Guid nodeId);

    Task<int> GetNodeDepth(Guid nodeId);

    Task RepairNestedSetValues();

    Task FixRootValues();

    Task<IEnumerable<Group>> GetRoots();
}
