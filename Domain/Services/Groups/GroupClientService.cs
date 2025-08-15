using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Groups;

public class GroupClientService : IGetAllClientIdsFromGroupAndSubgroups
{
    private readonly DataBaseContext _context;

    public GroupClientService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetAllClientIdsFromGroupAndSubgroups(Guid groupId)
    {
        var groupExists = await _context.Group.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
        {
            throw new KeyNotFoundException($"Group with ID {groupId} was not found");
        }

        var allGroupIds = await GetAllSubGroupIds(new List<Guid> { groupId });
        return await GetClientIdsForGroups(allGroupIds);
    }

    public async Task<List<Guid>> GetAllClientIdsFromGroupsAndSubgroupsFromList(List<Guid> groupIds)
    {
        if (groupIds == null || !groupIds.Any())
        {
            return new List<Guid>();
        }

        var existingGroups = await _context.Group
            .Where(g => groupIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync();

        var missingGroups = groupIds.Except(existingGroups).ToList();
        if (missingGroups.Any())
        {
            throw new KeyNotFoundException($"Groups with IDs {string.Join(", ", missingGroups)} were not found");
        }

        var allGroupIds = await GetAllSubGroupIds(existingGroups);
        return await GetClientIdsForGroups(allGroupIds);
    }

    private async Task<HashSet<Guid>> GetAllSubGroupIds(List<Guid> initialGroupIds)
    {
        var allGroupIds = new HashSet<Guid>(initialGroupIds);
        var currentGroupIds = new List<Guid>(initialGroupIds);

        while (currentGroupIds.Any())
        {
            var childGroupIds = await _context.Group
                .AsNoTracking()
                .Where(g => g.Parent.HasValue && currentGroupIds.Contains(g.Parent.Value))
                .Select(g => g.Id)
                .ToListAsync();

            var newGroupIds = childGroupIds.Where(id => allGroupIds.Add(id)).ToList();
            currentGroupIds = newGroupIds;
        }

        return allGroupIds;
    }

    private async Task<List<Guid>> GetClientIdsForGroups(HashSet<Guid> groupIds)
    {
        return await _context.GroupItem
            .AsNoTracking()
            .Where(gi => gi.ClientId.HasValue && groupIds.Contains(gi.GroupId))
            .Select(gi => gi.ClientId.Value)
            .Distinct()
            .ToListAsync();
    }
}