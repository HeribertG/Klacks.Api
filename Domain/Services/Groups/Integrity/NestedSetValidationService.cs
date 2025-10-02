using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Groups.Integrity;

public class NestedSetValidationService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<NestedSetValidationService> _logger;

    public NestedSetValidationService(DataBaseContext context, ILogger<NestedSetValidationService> logger)
    {
        _context = context;
        _logger = logger;
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

        var lftValues = treeNodes.Select(g => g.Lft).ToList();
        var rgtValues = treeNodes.Select(g => g.Rgt).ToList();

        if (lftValues.Count != lftValues.Distinct().Count() ||
            rgtValues.Count != rgtValues.Distinct().Count())
        {
            _logger.LogWarning("Duplicate Lft/Rgt values found in tree {RootId}", rootId);
            return false;
        }

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
}
