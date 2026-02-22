// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Groups;

public class GroupTreeService : IGroupTreeService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<GroupTreeService> _logger;
    private readonly IGroupTreeDatabaseAdapter _databaseAdapter;

    public GroupTreeService(
        DataBaseContext context,
        ILogger<GroupTreeService> logger,
        IGroupTreeDatabaseAdapter databaseAdapter)
    {
        _context = context;
        _logger = logger;
        _databaseAdapter = databaseAdapter;
    }

    public async Task<Group> AddChildNodeAsync(Guid parentId, Group newGroup)
    {
        _logger.LogInformation("Adding child node to parent {ParentId} for new group {GroupName}", parentId, newGroup.Name);

        var parent = await _context.Group
            .Include(x => x.GroupItems)
            .Where(g => g.Id == parentId)
            .FirstOrDefaultAsync();

        if (parent == null)
        {
            _logger.LogWarning("Parent group with ID {ParentId} not found.", parentId);
            throw new KeyNotFoundException($"Parent group with ID {parentId} not found");
        }

        await _databaseAdapter.UpdateRgtValuesAsync(parent.Rgt, parent.Root ?? parent.Id, 2);
        await _databaseAdapter.UpdateLftValuesAsync(parent.Rgt, parent.Root ?? parent.Id, 2);

        newGroup.Lft = parent.Rgt;
        newGroup.Rgt = parent.Rgt + 1;
        newGroup.Parent = parent.Id;
        newGroup.Root = parent.Root ?? parent.Id;
        newGroup.CreateTime = DateTime.UtcNow;

        _context.Group.Add(newGroup);

        _logger.LogInformation("Child node {GroupName} added to parent {ParentId}.", newGroup.Name, parentId);
        return newGroup;
    }

    public async Task<Group> AddRootNodeAsync(Group newGroup)
    {
        _logger.LogInformation("Adding new root group: {GroupName}", newGroup.Name);

        var maxRgt = await _context.Group
            .Where(g => g.Root == null)
            .OrderByDescending(g => g.Rgt)
            .Select(g => (int?)g.Rgt)
            .FirstOrDefaultAsync() ?? 0;

        newGroup.Lft = maxRgt + 1;
        newGroup.Rgt = maxRgt + 2;
        newGroup.Parent = null;
        newGroup.Root = null;
        newGroup.CreateTime = DateTime.UtcNow;

        _context.Group.Add(newGroup);

        _logger.LogInformation("Root group {GroupName} prepared for addition.", newGroup.Name);
        return newGroup;
    }

    public async Task MoveNodeAsync(Guid nodeId, Guid newParentId)
    {
        _logger.LogInformation("Attempting to move group with ID: {NodeId} to new parent ID: {NewParentId}", nodeId, newParentId);

        var node = await _context.Group
            .Where(g => g.Id == nodeId)
            .FirstOrDefaultAsync();

        if (node == null)
        {
            _logger.LogWarning("MoveNode: Group to be moved with ID {NodeId} not found.", nodeId);
            throw new KeyNotFoundException($"Group to be moved with ID {nodeId} not found");
        }

        var newParent = await _context.Group
            .Where(g => g.Id == newParentId)
            .FirstOrDefaultAsync();

        if (newParent == null)
        {
            _logger.LogWarning("MoveNode: New parent group with ID {NewParentId} not found.", newParentId);
            throw new KeyNotFoundException($"New parent group with ID {newParentId} not found");
        }

        if (!ValidateTreeMovement(node, newParent))
        {
            _logger.LogWarning("MoveNode: Invalid operation - new parent {NewParentId} is a descendant of node {NodeId}.", newParentId, nodeId);
            throw new InvalidOperationException("The new parent cannot be a descendant of the node to be moved");
        }

        var nodeWidth = node.Rgt - node.Lft + 1;
        var newPos = newParent.Rgt;

        await _databaseAdapter.MarkSubtreeWithNegativeValuesAsync(node.Lft, node.Rgt, node.Root ?? node.Id);

        await _databaseAdapter.ShiftNodesToCloseGapAsync(node.Rgt, node.Root ?? node.Id, nodeWidth);

        if (newPos > node.Rgt)
        {
            newPos -= nodeWidth;
        }

        await _databaseAdapter.MakeSpaceAtPositionAsync(newPos, newParent.Root ?? newParent.Id, nodeWidth);

        var offset = newPos - node.Lft;
        await _databaseAdapter.MoveMarkedSubtreeAsync(offset, newParent.Root ?? newParent.Id, node.Root ?? node.Id);

        node.Parent = newParentId;
        _context.Group.Update(node);

        _logger.LogInformation("Group with ID: {NodeId} moved to new parent {NewParentId}.", nodeId, newParentId);
    }

    public async Task<int> DeleteNodeAsync(Guid nodeId)
    {
        _logger.LogInformation("Attempting to delete group with ID: {NodeId}", nodeId);

        var groupEntity = await _context.Group
            .Where(g => g.Id == nodeId)
            .FirstOrDefaultAsync();

        if (groupEntity == null)
        {
            _logger.LogWarning("Group with ID: {NodeId} not found for deletion.", nodeId);
            throw new KeyNotFoundException($"Group with ID {nodeId} not found.");
        }

        var width = CalculateTreeWidth(groupEntity.Lft, groupEntity.Rgt);

        await _databaseAdapter.MarkSubtreeAsDeletedAsync(
            groupEntity.Lft, groupEntity.Rgt, groupEntity.Root ?? groupEntity.Id, DateTime.UtcNow);

        await _databaseAdapter.ShiftNodesAfterDeleteAsync(
            groupEntity.Rgt, groupEntity.Root ?? groupEntity.Id, width);

        _logger.LogInformation("Group with ID: {NodeId} and its subtree deleted.", nodeId);
        return width;
    }

    public int CalculateTreeWidth(int leftValue, int rightValue)
    {
        return rightValue - leftValue + 1;
    }

    public bool ValidateTreeMovement(Group nodeToMove, Group newParent)
    {
        return !(newParent.Lft > nodeToMove.Lft && newParent.Rgt < nodeToMove.Rgt);
    }

    public async Task RepairNestedSetValuesAsync(Guid rootId)
    {
        _logger.LogInformation("Starting RepairNestedSetValues for root {RootId}.", rootId);

        var rootNode = await _context.Group
            .Where(g => g.Id == rootId)
            .FirstOrDefaultAsync();

        if (rootNode == null)
        {
            throw new KeyNotFoundException($"Root node with ID {rootId} not found");
        }

        _logger.LogInformation("RepairNestedSetValues would rebuild tree structure for root {RootId}.", rootId);
    }

    public async Task UpdateTreePositionsAsync(Guid rootId, int startPosition, int adjustment)
    {
        _logger.LogInformation("Updating tree positions for root {RootId} from position {StartPosition} with adjustment {Adjustment}.",
            rootId, startPosition, adjustment);

        var affectedNodes = await _context.Group
            .Where(g => g.Root == rootId && (g.Lft >= startPosition || g.Rgt >= startPosition))
            .CountAsync();

        _logger.LogInformation("Would update {Count} nodes in tree {RootId}.", affectedNodes, rootId);
    }
}