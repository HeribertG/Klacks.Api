using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Repositories;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class GroupApplicationService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GroupApplicationService> _logger;

    public GroupApplicationService(
        IGroupRepository groupRepository,
        IMapper mapper,
        ILogger<GroupApplicationService> logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GroupResource?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.Get(id);
        return group != null ? _mapper.Map<GroupResource>(group) : null;
    }

    public async Task<TruncatedGroupResource> SearchGroupsAsync(GroupFilter filter, CancellationToken cancellationToken = default)
    {
        var searchCriteria = _mapper.Map<GroupSearchCriteria>(filter);
        var truncatedResult = await _groupRepository.Truncated(filter);
        var groups = truncatedResult.Groups;
        
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

    public async Task<GroupResource> CreateGroupAsync(GroupResource groupResource, CancellationToken cancellationToken = default)
    {
        var group = _mapper.Map<Group>(groupResource);
        await _groupRepository.Add(group);
        return _mapper.Map<GroupResource>(group);
    }

    public async Task<GroupResource> UpdateGroupAsync(GroupResource groupResource, CancellationToken cancellationToken = default)
    {
        var group = _mapper.Map<Group>(groupResource);
        var updatedGroup = await _groupRepository.Put(group);
        return _mapper.Map<GroupResource>(updatedGroup);
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _groupRepository.Delete(id);
    }

    public async Task<GroupTreeResource> GetGroupTreeAsync(Guid? rootId = null, CancellationToken cancellationToken = default)
    {
        var flatNodes = await _groupRepository.GetTree(rootId);
        var nodeDict = flatNodes.ToDictionary(g => g.Id, g => _mapper.Map<GroupResource>(g));
        var rootNodes = new List<GroupResource>();
        
        foreach (var node in nodeDict.Values)
        {
            if (node.Parent == null || !nodeDict.ContainsKey(node.Parent.Value))
            {
                rootNodes.Add(node);
                CalculateDepthRecursive(node, 0);
            }
            else
            {
                var parent = nodeDict[node.Parent.Value];
                parent.Children.Add(node);
            }
        }
        
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

    public async Task<List<GroupResource>> GetPathToNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var path = await _groupRepository.GetPath(nodeId);
        var pathList = path.ToList();
        var result = new List<GroupResource>();
        for (int i = 0; i < pathList.Count; i++)
        {
            var groupResource = _mapper.Map<GroupResource>(pathList[i]);
            groupResource.Depth = i;
            result.Add(groupResource);
        }
        
        return result;
    }

    public async Task<List<GroupResource>> GetRootGroupsAsync(CancellationToken cancellationToken = default)
    {
        var roots = await _groupRepository.GetRoots();
        return roots.Select(g => _mapper.Map<GroupResource>(g)).ToList();
    }

    public async Task<GroupResource> MoveGroupNodeAsync(Guid nodeId, Guid newParentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Move node {NodeId} to new parent {NewParentId}", nodeId, newParentId);
        
        await _groupRepository.MoveNode(nodeId, newParentId);
        
        var movedGroup = await _groupRepository.Get(nodeId);
        if (movedGroup == null)
        {
            throw new KeyNotFoundException($"Group with ID {nodeId} not found after move");
        }
        
        var depth = await _groupRepository.GetNodeDepth(nodeId);
        var result = _mapper.Map<GroupResource>(movedGroup);
        result.Depth = depth;
        
        _logger.LogInformation("Node {NodeId} successfully moved to parent {NewParentId}", nodeId, newParentId);
        
        return result;
    }

    public async Task<List<GroupItemResource>> GetGroupMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.Get(groupId);
        
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID {groupId} not found");
        }
        
        return _mapper.Map<List<GroupItemResource>>(group.GroupItems);
    }

    public async Task RefreshTreeStructureAsync(CancellationToken cancellationToken = default)
    {
        await _groupRepository.RepairNestedSetValues();
    }
}