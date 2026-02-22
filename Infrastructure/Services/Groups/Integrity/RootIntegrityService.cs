using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Groups.Integrity;

public class RootIntegrityService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<RootIntegrityService> _logger;

    public RootIntegrityService(DataBaseContext context, ILogger<RootIntegrityService> logger)
    {
        _context = context;
        _logger = logger;
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
                    var newRootId = FindExistingRoot(group.Id, allGroups);
                    group.Root = newRootId;
                    _context.Group.Update(group);
                }
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("FixRootValues completed");
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

            if (group.Parent.HasValue && !allGroupIds.Contains(group.Parent.Value))
            {
                _logger.LogWarning("Group {GroupId} has invalid parent reference {ParentId}",
                    group.Id, group.Parent.Value);
                group.Parent = null;
                wasFixed = true;
            }

            if (group.Root.HasValue && !allGroupIds.Contains(group.Root.Value))
            {
                _logger.LogWarning("Group {GroupId} has invalid root reference {RootId}",
                    group.Id, group.Root.Value);
                group.Root = FindExistingRoot(group.Id, allGroups);
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

    private Guid? FindExistingRoot(Guid nodeId, List<Group> allGroups)
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

        return FindExistingRoot(currentNode.Parent.Value, allGroups);
    }
}
