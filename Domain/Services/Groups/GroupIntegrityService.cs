using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Groups;

public class GroupIntegrityService : IGroupIntegrityService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<GroupIntegrityService> _logger;

    public GroupIntegrityService(DataBaseContext context, ILogger<GroupIntegrityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RepairNestedSetValuesAsync(Guid? rootId = null)
    {
        _logger.LogInformation("Starting RepairNestedSetValues for root {RootId}", rootId?.ToString() ?? "all");

        if (rootId.HasValue)
        {
            await RepairSingleTreeAsync(rootId.Value);
        }
        else
        {
            await RepairAllTreesAsync();
        }

        _logger.LogInformation("RepairNestedSetValues completed");
    }

    public async Task FixRootValuesAsync()
    {
        _logger.LogInformation("Starting FixRootValues");

        var allGroups = await _context.Group.ToListAsync();
        var groupsByRoot = allGroups
            .Where(g => g.Root != null)
            .GroupBy(g => g.Root)
            .ToList();

        foreach (var rootGroup in groupsByRoot)
        {
            var rootId = rootGroup.Key;
            var rootExists = allGroups.Any(g => g.Id == rootId);

            if (!rootExists)
            {
                _logger.LogWarning("Root {RootId} does not exist, fixing references", rootId);

                foreach (var group in rootGroup)
                {
                    var newRootId = await FindExistingRootAsync(group.Id, allGroups);
                    group.Root = newRootId;
                    _context.Group.Update(group);
                }
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("FixRootValues completed");
    }

    public async Task<bool> ValidateNestedSetIntegrityAsync(Guid rootId)
    {
        _logger.LogInformation("Validating nested set integrity for root {RootId}", rootId);

        var treeNodes = await _context.Group
            .Where(g => g.Root == rootId || g.Id == rootId)
            .OrderBy(g => g.Lft)
            .ToListAsync();

        if (!treeNodes.Any())
        {
            _logger.LogWarning("No nodes found for root {RootId}", rootId);
            return true;
        }

        // Check for unique Lft/Rgt values
        var lftValues = treeNodes.Select(g => g.Lft).ToList();
        var rgtValues = treeNodes.Select(g => g.Rgt).ToList();

        if (lftValues.Count != lftValues.Distinct().Count() || 
            rgtValues.Count != rgtValues.Distinct().Count())
        {
            _logger.LogWarning("Duplicate Lft/Rgt values found in tree {RootId}", rootId);
            return false;
        }

        // Check proper ordering and boundaries
        foreach (var node in treeNodes)
        {
            if (node.Lft >= node.Rgt)
            {
                _logger.LogWarning("Invalid Lft/Rgt values for node {NodeId}: Lft={Lft}, Rgt={Rgt}", 
                    node.Id, node.Lft, node.Rgt);
                return false;
            }
        }

        _logger.LogInformation("Nested set integrity validation passed for root {RootId}", rootId);
        return true;
    }

    public async Task RebuildSubtreeAsync(Guid rootId)
    {
        _logger.LogInformation("Rebuilding subtree for root {RootId}", rootId);

        var rootNode = await _context.Group
            .Where(g => g.Id == rootId)
            .FirstOrDefaultAsync();

        if (rootNode == null)
        {
            _logger.LogWarning("Root node {RootId} not found", rootId);
            throw new KeyNotFoundException($"Root node with ID {rootId} not found");
        }

        int counter = 1;
        rootNode.Lft = counter++;
        counter = await RebuildSubtreeRecursiveAsync(rootId, counter);
        rootNode.Rgt = counter++;

        if (rootNode.Root != null)
        {
            rootNode.Root = rootNode.Id;
        }

        _context.Group.Update(rootNode);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Subtree rebuild completed for root {RootId}", rootId);
    }

    public async Task<IEnumerable<Group>> FindIntegrityIssuesAsync()
    {
        _logger.LogInformation("Finding integrity issues across all groups");

        var issueGroups = new List<Group>();

        // Find orphaned groups
        var orphaned = await FindOrphanedGroupsAsync();
        issueGroups.AddRange(orphaned);

        // Find circular references
        var circular = await FindCircularReferencesAsync();
        issueGroups.AddRange(circular);

        // Find nested set inconsistencies
        var roots = await _context.Group
            .Where(g => g.Parent == null)
            .Select(g => g.Id)
            .ToListAsync();

        foreach (var rootId in roots)
        {
            var isValid = await ValidateNestedSetIntegrityAsync(rootId);
            if (!isValid)
            {
                var treeNodes = await _context.Group
                    .Where(g => g.Root == rootId || g.Id == rootId)
                    .ToListAsync();
                issueGroups.AddRange(treeNodes);
            }
        }

        var uniqueIssues = issueGroups.Distinct().ToList();
        _logger.LogInformation("Found {Count} groups with integrity issues", uniqueIssues.Count);
        
        return uniqueIssues;
    }

    public IEnumerable<string> ValidateGroupData(Group group)
    {
        _logger.LogDebug("Validating group data for {GroupId}", group.Id);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(group.Name))
        {
            errors.Add("Group name is required");
        }

        if (group.ValidFrom == default)
        {
            errors.Add("ValidFrom date is required");
        }

        if (group.ValidUntil.HasValue && group.ValidFrom > group.ValidUntil.Value)
        {
            errors.Add("ValidFrom must be before ValidUntil");
        }

        if (group.Lft >= group.Rgt)
        {
            errors.Add($"Invalid nested set values: Lft ({group.Lft}) must be less than Rgt ({group.Rgt})");
        }

        _logger.LogDebug("Group {GroupId} validation found {ErrorCount} errors", group.Id, errors.Count);
        return errors;
    }

    public async Task EnsureReferentialIntegrityAsync()
    {
        _logger.LogInformation("Ensuring referential integrity");

        var allGroups = await _context.Group.ToListAsync();
        var allGroupIds = allGroups.Select(g => g.Id).ToHashSet();

        var fixedCount = 0;

        foreach (var group in allGroups)
        {
            var wasFixed = false;

            // Check parent reference
            if (group.Parent.HasValue && !allGroupIds.Contains(group.Parent.Value))
            {
                _logger.LogWarning("Group {GroupId} has invalid parent reference {ParentId}", 
                    group.Id, group.Parent.Value);
                group.Parent = null;
                wasFixed = true;
            }

            // Check root reference
            if (group.Root.HasValue && !allGroupIds.Contains(group.Root.Value))
            {
                _logger.LogWarning("Group {GroupId} has invalid root reference {RootId}", 
                    group.Id, group.Root.Value);
                group.Root = await FindExistingRootAsync(group.Id, allGroups);
                wasFixed = true;
            }

            if (wasFixed)
            {
                _context.Group.Update(group);
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Referential integrity check completed, fixed {Count} groups", fixedCount);
    }

    public async Task<IEnumerable<Group>> FindOrphanedGroupsAsync()
    {
        _logger.LogInformation("Finding orphaned groups");

        var allGroups = await _context.Group.ToListAsync();
        var allGroupIds = allGroups.Select(g => g.Id).ToHashSet();

        var orphaned = allGroups
            .Where(g => g.Parent.HasValue && !allGroupIds.Contains(g.Parent.Value))
            .ToList();

        _logger.LogInformation("Found {Count} orphaned groups", orphaned.Count);
        return orphaned;
    }

    public async Task<bool> ValidateLftRgtUniquenessAsync(Guid rootId)
    {
        _logger.LogInformation("Validating Lft/Rgt uniqueness for root {RootId}", rootId);

        var treeNodes = await _context.Group
            .Where(g => g.Root == rootId || g.Id == rootId)
            .ToListAsync();

        var lftValues = treeNodes.Select(g => g.Lft).ToList();
        var rgtValues = treeNodes.Select(g => g.Rgt).ToList();

        var lftUnique = lftValues.Count == lftValues.Distinct().Count();
        var rgtUnique = rgtValues.Count == rgtValues.Distinct().Count();

        var isValid = lftUnique && rgtUnique;
        _logger.LogInformation("Lft/Rgt uniqueness validation for root {RootId}: {IsValid}", rootId, isValid);

        return isValid;
    }

    public async Task<IEnumerable<Group>> FindCircularReferencesAsync()
    {
        _logger.LogInformation("Finding circular references");

        var allGroups = await _context.Group.ToListAsync();
        var circularGroups = new HashSet<Group>();

        foreach (var group in allGroups)
        {
            if (await HasCircularReferenceAsync(group, allGroups))
            {
                circularGroups.Add(group);
            }
        }

        _logger.LogInformation("Found {Count} groups with circular references", circularGroups.Count);
        return circularGroups;
    }

    public async Task<bool> ValidateNestedSetBoundariesAsync(Guid rootId)
    {
        _logger.LogInformation("Validating nested set boundaries for root {RootId}", rootId);

        var treeNodes = await _context.Group
            .Where(g => g.Root == rootId || g.Id == rootId)
            .ToListAsync();

        foreach (var parent in treeNodes)
        {
            var children = treeNodes.Where(g => g.Parent == parent.Id).ToList();
            
            foreach (var child in children)
            {
                if (!(child.Lft > parent.Lft && child.Rgt < parent.Rgt))
                {
                    _logger.LogWarning("Nested set boundary violation: Child {ChildId} not properly contained within parent {ParentId}", 
                        child.Id, parent.Id);
                    return false;
                }
            }
        }

        _logger.LogInformation("Nested set boundaries validation passed for root {RootId}", rootId);
        return true;
    }

    public async Task<GroupIntegrityReport> PerformFullIntegrityCheckAsync()
    {
        _logger.LogInformation("Perform" +
            "ing full integrity check");

        var report = new GroupIntegrityReport
        {
            OrphanedGroups = await FindOrphanedGroupsAsync(),
            CircularReferences = await FindCircularReferencesAsync()
        };

        var validationErrors = new List<string>();
        var nestedSetInconsistencies = new List<Group>();

        var allGroups = await _context.Group.ToListAsync();
        
        foreach (var group in allGroups)
        {
            var groupErrors = ValidateGroupData(group);
            validationErrors.AddRange(groupErrors.Select(e => $"Group {group.Id}: {e}"));
        }

        var roots = await _context.Group
            .Where(g => g.Parent == null)
            .Select(g => g.Id)
            .ToListAsync();

        foreach (var rootId in roots)
        {
            var isValid = await ValidateNestedSetIntegrityAsync(rootId);
            if (!isValid)
            {
                var treeNodes = await _context.Group
                    .Where(g => g.Root == rootId || g.Id == rootId)
                    .ToListAsync();
                nestedSetInconsistencies.AddRange(treeNodes);
            }
        }

        report.ValidationErrors = validationErrors;
        report.NestedSetInconsistencies = nestedSetInconsistencies;

        _logger.LogInformation("Full integrity check completed. Valid: {IsValid}", report.IsIntegrityValid);
        return report;
    }

    private async Task RepairSingleTreeAsync(Guid rootId)
    {
        var rootNode = await _context.Group
            .Where(g => g.Id == rootId)
            .FirstOrDefaultAsync();

        if (rootNode == null)
        {
            throw new KeyNotFoundException($"Root node with ID {rootId} not found");
        }

        await RebuildSubtreeAsync(rootId);
    }

    private async Task RepairAllTreesAsync()
    {
        var rootNodes = await _context.Group
            .Where(g => g.Parent == null)
            .OrderBy(g => g.Id)
            .ToListAsync();

        foreach (var rootNode in rootNodes)
        {
            await RebuildSubtreeAsync(rootNode.Id);
        }
    }

    private async Task<int> RebuildSubtreeRecursiveAsync(Guid parentId, int counter)
    {
        var children = await _context.Group
            .Where(g => g.Parent == parentId)
            .OrderBy(g => g.Name)
            .ToListAsync();

        foreach (var child in children)
        {
            child.Lft = counter++;
            counter = await RebuildSubtreeRecursiveAsync(child.Id, counter);
            child.Rgt = counter++;

            var rootId = await GetRootIdAsync(child.Id);
            child.Root = rootId;

            _context.Group.Update(child);
        }

        return counter;
    }

    private async Task<Guid?> GetRootIdAsync(Guid nodeId)
    {
        var node = await _context.Group
            .Where(g => g.Id == nodeId)
            .FirstOrDefaultAsync();

        if (node == null || node.Parent == null)
        {
            return node?.Id;
        }

        return await GetRootIdAsync(node.Parent.Value);
    }

    private async Task<Guid?> FindExistingRootAsync(Guid nodeId, List<Group> allGroups)
    {
        var currentNode = allGroups.FirstOrDefault(g => g.Id == nodeId);

        if (currentNode == null || currentNode.Parent == null)
        {
            return currentNode?.Id;
        }

        var parentExists = allGroups.Any(g => g.Id == currentNode.Parent.Value);
        if (!parentExists)
        {
            return currentNode.Id;
        }

        return await FindExistingRootAsync(currentNode.Parent.Value, allGroups);
    }

    private async Task<bool> HasCircularReferenceAsync(Group group, List<Group> allGroups)
    {
        var visited = new HashSet<Guid>();
        Guid? currentId = group.Id;

        while (currentId.HasValue)
        {
            if (visited.Contains(currentId.Value))
            {
                return true; // Circular reference detected
            }

            visited.Add(currentId.Value);
            var current = allGroups.FirstOrDefault(g => g.Id == currentId.Value);
            currentId = current?.Parent;
        }

        return false;
    }
}