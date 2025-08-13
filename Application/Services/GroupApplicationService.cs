using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Repositories;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services;

/// <summary>
/// Application Service for Group operations following Clean Architecture principles
/// Orchestrates between Presentation DTOs and Domain Models/Services
/// </summary>
public class GroupApplicationService
{
    private readonly IGroupRepository _groupRepository; // Current repository (Phase 1 already refactored)
    // private readonly IGroupDomainRepository _groupDomainRepository; // Phase 2 - TODO: Implement
    private readonly IMapper _mapper;

    public GroupApplicationService(
        IGroupRepository groupRepository,
        // IGroupDomainRepository groupDomainRepository, // Phase 2
        IMapper mapper)
    {
        _groupRepository = groupRepository;
        // _groupDomainRepository = groupDomainRepository; // Phase 2
        _mapper = mapper;
    }

    /// <summary>
    /// Get a single group by ID - demonstrates proper mapping
    /// </summary>
    public async Task<GroupResource?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository
        // var group = await _groupDomainRepository.GetByIdAsync(id, cancellationToken);
        
        // Phase 1: Use existing repository
        var group = await _groupRepository.Get(id);
        
        return group != null ? _mapper.Map<GroupResource>(group) : null;
    }

    /// <summary>
    /// Search groups with filtering - demonstrates DTO to Criteria mapping
    /// </summary>
    public async Task<TruncatedGroupResource> SearchGroupsAsync(GroupFilter filter, CancellationToken cancellationToken = default)
    {
        // Phase 3: Map Presentation Filter to Domain Criteria
        var searchCriteria = _mapper.Map<GroupSearchCriteria>(filter);
        
        // TODO Phase 2: Use Domain Repository
        // var pagedResult = await _groupDomainRepository.SearchAsync(searchCriteria, cancellationToken);
        
        // Phase 1: Use existing repository (temporary)
        var truncatedResult = await _groupRepository.Truncated(filter);
        var groups = truncatedResult.Groups;
        
        // Phase 1: Use existing repository pattern 
        // TODO Phase 2: Replace with Domain PagedResult pattern
        var truncatedGroupResource = new TruncatedGroupResource
        {
            Groups = _mapper.Map<List<GroupResource>>(groups),
            MaxItems = truncatedResult.MaxItems,
            MaxPages = truncatedResult.MaxPages,
            CurrentPage = truncatedResult.CurrentPage,
            FirstItemOnPage = truncatedResult.FirstItemOnPage
        };
        
        return truncatedGroupResource;
    }

    /// <summary>
    /// Create a new group - demonstrates DTO to Domain Model mapping
    /// </summary>
    public async Task<GroupResource> CreateGroupAsync(GroupResource groupResource, CancellationToken cancellationToken = default)
    {
        // Map Presentation DTO to Domain Model
        var group = _mapper.Map<Group>(groupResource);
        
        // Phase 2: Use Domain Repository
        // var createdGroup = await _groupDomainRepository.AddAsync(group, cancellationToken);
        
        // Phase 1: Use existing repository
        await _groupRepository.Add(group);
        
        // Map back to Presentation DTO
        return _mapper.Map<GroupResource>(group);
    }

    /// <summary>
    /// Update an existing group - demonstrates bidirectional mapping
    /// </summary>
    public async Task<GroupResource> UpdateGroupAsync(GroupResource groupResource, CancellationToken cancellationToken = default)
    {
        // Map Presentation DTO to Domain Model
        var group = _mapper.Map<Group>(groupResource);
        
        // Phase 2: Use Domain Repository
        // var updatedGroup = await _groupDomainRepository.UpdateAsync(group, cancellationToken);
        
        // Phase 1: Use existing repository
        var updatedGroup = await _groupRepository.Put(group);
        
        // Map back to Presentation DTO
        return _mapper.Map<GroupResource>(updatedGroup);
    }

    /// <summary>
    /// Delete a group by ID
    /// </summary>
    public async Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository
        // await _groupDomainRepository.DeleteAsync(id, cancellationToken);
        
        // Phase 1: Use existing repository
        await _groupRepository.Delete(id);
    }

    /// <summary>
    /// Get group tree structure - demonstrates hierarchical operations
    /// </summary>
    public async Task<GroupTreeResource> GetGroupTreeAsync(Guid? rootId = null, CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository with tree operations
        // var treeResult = await _groupDomainRepository.GetTreeAsync(rootId, cancellationToken);
        
        // Phase 1: Use existing repository
        var flatNodes = await _groupRepository.GetTree(rootId);
        
        // Build hierarchical tree structure from flat list
        var nodeDict = flatNodes.ToDictionary(g => g.Id, g => _mapper.Map<GroupResource>(g));
        var rootNodes = new List<GroupResource>();
        
        foreach (var node in nodeDict.Values)
        {
            if (node.Parent == null || !nodeDict.ContainsKey(node.Parent.Value))
            {
                // This is a root node
                rootNodes.Add(node);
                CalculateDepthRecursive(node, 0);
            }
            else
            {
                // Add to parent's children
                var parent = nodeDict[node.Parent.Value];
                parent.Children.Add(node);
            }
        }
        
        // Sort children at each level by Lft value for proper tree ordering
        SortChildrenRecursive(rootNodes);
        
        return new GroupTreeResource
        {
            RootId = rootId,
            Nodes = rootNodes
        };
    }
    
    private void CalculateDepthRecursive(GroupResource node, int depth)
    {
        node.Depth = depth;
        foreach (var child in node.Children)
        {
            CalculateDepthRecursive(child, depth + 1);
        }
    }
    
    private void SortChildrenRecursive(List<GroupResource> nodes)
    {
        var sortedNodes = nodes.OrderBy(n => n.Lft).ToList();
        nodes.Clear();
        nodes.AddRange(sortedNodes);
        
        foreach (var node in nodes)
        {
            if (node.Children.Any())
            {
                SortChildrenRecursive(node.Children);
            }
        }
    }

    /// <summary>
    /// Get path to a specific group node
    /// </summary>
    public async Task<List<GroupResource>> GetPathToNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository
        // var pathNodes = await _groupDomainRepository.GetPathAsync(nodeId, cancellationToken);
        
        // Phase 1: Use existing repository
        var path = await _groupRepository.GetPath(nodeId);
        
        // Map with calculated depth based on position in path (0-based depth)
        var pathList = path.ToList();
        var result = new List<GroupResource>();
        for (int i = 0; i < pathList.Count; i++)
        {
            var groupResource = _mapper.Map<GroupResource>(pathList[i]);
            groupResource.Depth = i; // Set depth based on position in path
            result.Add(groupResource);
        }
        
        return result;
    }

    /// <summary>
    /// Get root groups - demonstrates hierarchical query
    /// </summary>
    public async Task<List<GroupResource>> GetRootGroupsAsync(CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository
        // var rootGroups = await _groupDomainRepository.GetRootsAsync(cancellationToken);
        
        // Phase 1: Use existing repository
        var roots = await _groupRepository.GetRoots();
        
        return roots.Select(g => _mapper.Map<GroupResource>(g)).ToList();
    }

    /// <summary>
    /// Move a group node in the hierarchy - complex domain operation
    /// </summary>
    public async Task MoveGroupNodeAsync(Guid nodeId, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository with tree services
        // await _groupDomainRepository.MoveNodeAsync(nodeId, newParentId, cancellationToken);
        
        // Phase 1: Use existing repository
        await _groupRepository.MoveNode(nodeId, newParentId ?? Guid.Empty);
    }

    /// <summary>
    /// Get group members (clients in group)
    /// </summary>
    public async Task<List<GroupResource>> GetGroupMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository
        // var members = await _groupDomainRepository.GetMembersAsync(groupId, cancellationToken);
        
        // Phase 1: Use existing repository
        var children = await _groupRepository.GetChildren(groupId);
        
        return children.Select(g => _mapper.Map<GroupResource>(g)).ToList();
    }

    /// <summary>
    /// Refresh/repair tree structure - maintenance operation
    /// </summary>
    public async Task RefreshTreeStructureAsync(CancellationToken cancellationToken = default)
    {
        // Phase 2: Use Domain Repository with integrity services
        // await _groupDomainRepository.RefreshTreeAsync(cancellationToken);
        
        // Phase 1: Use existing repository
        await _groupRepository.RepairNestedSetValues();
    }
}