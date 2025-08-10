using Klacks.Api.Datas;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Models.Associations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Groups;

public class GroupHierarchyService : IGroupHierarchyService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<GroupHierarchyService> _logger;

    public GroupHierarchyService(DataBaseContext context, ILogger<GroupHierarchyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Group>> GetChildrenAsync(Guid parentId)
    {
        _logger.LogInformation("Getting children for parent group {ParentId}", parentId);

        var parent = await _context.Group
            .Where(g => g.Id == parentId)
            .FirstOrDefaultAsync();

        if (parent == null)
        {
            _logger.LogWarning("Parent group with ID {ParentId} not found.", parentId);
            throw new KeyNotFoundException($"Parent group with ID {parentId} not found");
        }

        var children = await _context.Group
            .Where(g => g.Parent == parentId)
            .OrderBy(g => g.Lft)
            .ToListAsync();

        _logger.LogInformation("Found {Count} children for parent group {ParentId}", children.Count, parentId);
        return children;
    }

    public async Task<int> GetNodeDepthAsync(Guid nodeId)
    {
        _logger.LogInformation("Calculating depth for node {NodeId}", nodeId);

        var node = await _context.Group
            .Where(g => g.Id == nodeId)
            .FirstOrDefaultAsync();

        if (node == null)
        {
            _logger.LogWarning("Node with ID {NodeId} not found.", nodeId);
            throw new KeyNotFoundException($"Group with ID {nodeId} not found");
        }

        if (node.Parent == null)
        {
            _logger.LogInformation("Node {NodeId} is a root node with depth 0", nodeId);
            return 0;
        }

        var depth = await _context.Group
            .CountAsync(g => g.Lft < node.Lft &&
                           g.Rgt > node.Rgt &&
                           (g.Root == node.Root || (g.Root == null && g.Id == node.Root)));

        _logger.LogInformation("Node {NodeId} has depth {Depth}", nodeId, depth);
        return depth;
    }

    public async Task<IEnumerable<Group>> GetPathAsync(Guid nodeId)
    {
        _logger.LogInformation("Getting path for node {NodeId}", nodeId);

        var node = await _context.Group
            .Where(g => g.Id == nodeId)
            .FirstOrDefaultAsync();

        if (node == null)
        {
            _logger.LogWarning("Node with ID {NodeId} not found.", nodeId);
            throw new KeyNotFoundException($"Group with ID {nodeId} not found");
        }

        var path = await _context.Group
            .Where(g => g.Lft <= node.Lft && g.Rgt >= node.Rgt && g.Root == node.Root)
            .OrderBy(g => g.Lft)
            .ToListAsync();

        _logger.LogInformation("Found path with {Count} groups for node {NodeId}", path.Count, nodeId);
        return path;
    }

    public async Task<IEnumerable<Group>> GetTreeAsync(Guid? rootId = null)
    {
        _logger.LogInformation("Getting tree structure for root {RootId}", rootId?.ToString() ?? "all roots");

        var today = DateTime.UtcNow.Date;

        if (rootId.HasValue)
        {
            var root = await _context.Group
                .Where(g => g.Id == rootId)
                .FirstOrDefaultAsync();

            if (root == null)
            {
                _logger.LogWarning("Root group with ID {RootId} not found.", rootId);
                throw new KeyNotFoundException($"Root group with ID {rootId} not found");
            }

            var treeNodes = await _context.Group
                .Where(g => g.Root == rootId || g.Id == rootId)
                .Include(g => g.GroupItems.Where(gi => !gi.ShiftId.HasValue && gi.ClientId.HasValue))
                .ThenInclude(gi => gi.Client)
                .ToListAsync();

            var validNodes = treeNodes
                .Where(g => g.ValidFrom.Date <= today &&
                           (!g.ValidUntil.HasValue || g.ValidUntil.Value.Date >= today))
                .OrderBy(g => g.Name)
                .ThenBy(g => g.Root)
                .ThenBy(g => g.Lft)
                .ToList();

            _logger.LogInformation("Found {Count} valid nodes in tree {RootId}", validNodes.Count, rootId);
            return validNodes;
        }
        else
        {
            var allNodes = await _context.Group
                .Include(g => g.GroupItems.Where(gi => !gi.ShiftId.HasValue && gi.ClientId.HasValue))
                .ThenInclude(gi => gi.Client)
                .ToListAsync();

            var validNodes = allNodes
                .Where(g => g.ValidFrom.Date <= today &&
                           (!g.ValidUntil.HasValue || g.ValidUntil.Value.Date >= today))
                .OrderBy(g => g.Name)
                .ThenBy(g => g.Root)
                .ThenBy(g => g.Lft)
                .ToList();

            _logger.LogInformation("Found {Count} valid nodes across all trees", validNodes.Count);
            return validNodes;
        }
    }

    public async Task<IEnumerable<Group>> GetRootsAsync()
    {
        _logger.LogInformation("Getting all root groups");

        var roots = await _context.Group
            .AsNoTracking()
            .Where(g => g.Id == g.Root || !(g.Root.HasValue && g.Parent.HasValue))
            .OrderBy(g => g.Name)
            .ToListAsync();

        _logger.LogInformation("Found {Count} root groups", roots.Count);
        return roots;
    }

    public async Task<bool> IsAncestorOfAsync(Guid ancestorId, Guid descendantId)
    {
        _logger.LogInformation("Checking if {AncestorId} is ancestor of {DescendantId}", ancestorId, descendantId);

        if (ancestorId == descendantId)
        {
            _logger.LogInformation("Ancestor and descendant are the same - returning false");
            return false;
        }

        var ancestor = await _context.Group
            .Where(g => g.Id == ancestorId)
            .FirstOrDefaultAsync();

        var descendant = await _context.Group
            .Where(g => g.Id == descendantId)
            .FirstOrDefaultAsync();

        if (ancestor == null || descendant == null)
        {
            _logger.LogWarning("One or both groups not found: ancestor={AncestorExists}, descendant={DescendantExists}", 
                ancestor != null, descendant != null);
            return false;
        }

        // In nested set model, ancestor's Lft < descendant's Lft and ancestor's Rgt > descendant's Rgt
        var isAncestor = ancestor.Lft < descendant.Lft && 
                        ancestor.Rgt > descendant.Rgt && 
                        ancestor.Root == descendant.Root;

        _logger.LogInformation("IsAncestor result: {IsAncestor}", isAncestor);
        return isAncestor;
    }

    public async Task<IEnumerable<Group>> GetDescendantsAsync(Guid parentId, bool includeParent = false)
    {
        _logger.LogInformation("Getting descendants for parent {ParentId}, includeParent: {IncludeParent}", 
            parentId, includeParent);

        var parent = await _context.Group
            .Where(g => g.Id == parentId)
            .FirstOrDefaultAsync();

        if (parent == null)
        {
            _logger.LogWarning("Parent group with ID {ParentId} not found.", parentId);
            throw new KeyNotFoundException($"Parent group with ID {parentId} not found");
        }

        IQueryable<Group> query;
        if (includeParent)
        {
            query = _context.Group
                .Where(g => g.Lft >= parent.Lft && g.Rgt <= parent.Rgt && g.Root == parent.Root);
        }
        else
        {
            query = _context.Group
                .Where(g => g.Lft > parent.Lft && g.Rgt < parent.Rgt && g.Root == parent.Root);
        }

        var descendants = await query
            .OrderBy(g => g.Lft)
            .ToListAsync();

        _logger.LogInformation("Found {Count} descendants for parent {ParentId}", descendants.Count, parentId);
        return descendants;
    }

    public async Task<IEnumerable<Group>> GetAncestorsAsync(Guid nodeId, bool includeNode = false)
    {
        _logger.LogInformation("Getting ancestors for node {NodeId}, includeNode: {IncludeNode}", 
            nodeId, includeNode);

        var node = await _context.Group
            .Where(g => g.Id == nodeId)
            .FirstOrDefaultAsync();

        if (node == null)
        {
            _logger.LogWarning("Node with ID {NodeId} not found.", nodeId);
            throw new KeyNotFoundException($"Group with ID {nodeId} not found");
        }

        IQueryable<Group> query;
        if (includeNode)
        {
            query = _context.Group
                .Where(g => g.Lft <= node.Lft && g.Rgt >= node.Rgt && g.Root == node.Root);
        }
        else
        {
            query = _context.Group
                .Where(g => g.Lft < node.Lft && g.Rgt > node.Rgt && g.Root == node.Root);
        }

        var ancestors = await query
            .OrderBy(g => g.Lft)
            .ToListAsync();

        _logger.LogInformation("Found {Count} ancestors for node {NodeId}", ancestors.Count, nodeId);
        return ancestors;
    }
}