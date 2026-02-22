using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Groups.Integrity;

public class GroupIssueFindingService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<GroupIssueFindingService> _logger;
    private readonly NestedSetValidationService _validationService;

    public GroupIssueFindingService(
        DataBaseContext context,
        ILogger<GroupIssueFindingService> logger,
        NestedSetValidationService validationService)
    {
        _context = context;
        _logger = logger;
        _validationService = validationService;
    }

    public async Task<IEnumerable<Group>> FindIntegrityIssuesAsync()
    {
        _logger.LogInformation("Finding integrity issues across all groups");

        var issueGroups = new List<Group>();

        var orphaned = await FindOrphanedGroupsAsync();
        issueGroups.AddRange(orphaned);

        var circular = await FindCircularReferencesAsync();
        issueGroups.AddRange(circular);

        var roots = await _context.Group
            .Where(g => g.Parent == null)
            .Select(g => g.Id)
            .ToListAsync();

        foreach (var rootId in roots)
        {
            var isValid = await _validationService.ValidateNestedSetIntegrityAsync(rootId);
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

    public async Task<IEnumerable<Group>> FindCircularReferencesAsync()
    {
        _logger.LogInformation("Finding circular references");

        var allGroups = await _context.Group.ToListAsync();
        var circularGroups = new HashSet<Group>();

        foreach (var group in allGroups)
        {
            if (HasCircularReference(group, allGroups))
            {
                circularGroups.Add(group);
            }
        }

        _logger.LogInformation("Found {Count} groups with circular references", circularGroups.Count);
        return circularGroups;
    }

    public async Task<GroupIntegrityReport> PerformFullIntegrityCheckAsync()
    {
        _logger.LogInformation("Performing full integrity check");

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
            var groupErrors = _validationService.ValidateGroupData(group);
            validationErrors.AddRange(groupErrors.Select(e => $"Group {group.Id}: {e}"));
        }

        var roots = await _context.Group
            .Where(g => g.Parent == null)
            .Select(g => g.Id)
            .ToListAsync();

        foreach (var rootId in roots)
        {
            var isValid = await _validationService.ValidateNestedSetIntegrityAsync(rootId);
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

    private bool HasCircularReference(Group group, List<Group> allGroups)
    {
        var visited = new HashSet<Guid>();
        Guid? currentId = group.Id;

        while (currentId.HasValue)
        {
            if (visited.Contains(currentId.Value))
            {
                return true;
            }

            visited.Add(currentId.Value);
            var current = allGroups.FirstOrDefault(g => g.Id == currentId.Value);
            currentId = current?.Parent;
        }

        return false;
    }
}
