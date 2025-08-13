using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;

namespace Klacks.Api.Domain.Interfaces.Repositories;

public interface IGroupDomainRepository
{
    Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<GroupSummary>> SearchAsync(GroupSearchCriteria criteria, CancellationToken cancellationToken = default);
    
    Task<List<GroupSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default);
    
    Task<Group> UpdateAsync(Group group, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    
    // Hierarchy-specific methods
    Task<List<GroupSummary>> GetRootsAsync(CancellationToken cancellationToken = default);
    
    Task<List<GroupSummary>> GetChildrenAsync(int parentId, CancellationToken cancellationToken = default);
    
    Task<List<GroupSummary>> GetDescendantsAsync(int groupId, int? maxDepth = null, CancellationToken cancellationToken = default);
    
    Task<List<GroupSummary>> GetAncestorsAsync(int groupId, CancellationToken cancellationToken = default);
    
    Task<List<GroupSummary>> GetPathAsync(int groupId, CancellationToken cancellationToken = default);
    
    Task<int> GetDepthAsync(int groupId, CancellationToken cancellationToken = default);
    
    Task<bool> IsAncestorOfAsync(int ancestorId, int descendantId, CancellationToken cancellationToken = default);
    
    // Tree structure methods
    Task<Group> AddChildAsync(Group parent, Group child, CancellationToken cancellationToken = default);
    
    Task<Group> AddRootAsync(Group group, CancellationToken cancellationToken = default);
    
    Task MoveNodeAsync(int nodeId, int? newParentId, CancellationToken cancellationToken = default);
    
    Task DeleteSubtreeAsync(int nodeId, CancellationToken cancellationToken = default);
    
    // Validity-specific methods
    Task<List<GroupSummary>> GetActiveGroupsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    
    Task<List<GroupSummary>> GetFormerGroupsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    
    Task<List<GroupSummary>> GetFutureGroupsAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    
    Task<List<GroupSummary>> GetGroupsValidOnDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    
    Task<List<GroupSummary>> GetGroupsExpiringWithinAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    
    // Membership methods
    Task<List<int>> GetAllClientIdsFromGroupAndSubgroupsAsync(int groupId, CancellationToken cancellationToken = default);
    
    Task<int> GetMemberCountAsync(int groupId, bool includeSubGroups = false, CancellationToken cancellationToken = default);
    
    Task AddClientToGroupAsync(int groupId, int clientId, CancellationToken cancellationToken = default);
    
    Task RemoveClientFromGroupAsync(int groupId, int clientId, CancellationToken cancellationToken = default);
    
    Task<bool> IsClientInGroupAsync(int groupId, int clientId, CancellationToken cancellationToken = default);
    
    // Data integrity methods
    Task<bool> ValidateTreeIntegrityAsync(CancellationToken cancellationToken = default);
    
    Task RepairTreeStructureAsync(CancellationToken cancellationToken = default);
    
    Task<List<string>> FindIntegrityIssuesAsync(CancellationToken cancellationToken = default);
}