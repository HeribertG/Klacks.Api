using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Groups.Integrity;

public class NestedSetRepairService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<NestedSetRepairService> _logger;

    public NestedSetRepairService(DataBaseContext context, ILogger<NestedSetRepairService> logger)
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
}
