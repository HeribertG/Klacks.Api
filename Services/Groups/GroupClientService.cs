using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Klacks.Api.Models.Associations;

namespace Klacks.Api.Services.Groups;

/// <summary>
/// Service for retrieving all ClientIds from groups and subgroups
/// </summary>
public class GroupClientService : IGetAllClientIdsFromGroupAndSubgroups
{
    private readonly DataBaseContext context;

    public GroupClientService(DataBaseContext context)
    {
        this.context = context;
    }

    public async Task<List<Guid>> GetAllClientIdsFromGroupAndSubgroups(Guid groupId)
    {
        HashSet<Guid> clientIds = new HashSet<Guid>();

        try
        {
            var mainGroup = await context.Group
                .Include(g => g.GroupItems)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (mainGroup == null)
            {
                throw new KeyNotFoundException($"Group with ID {groupId} was not found");
            }

            var allGroups = await context.Group
                .AsNoTracking()
                .Include(g => g.GroupItems)
                .ToListAsync();

            if (mainGroup.GroupItems != null)
            {
                foreach (var item in mainGroup.GroupItems.Where(x => x.ClientId.HasValue))
                {
                    if (item != null)
                    {
                        clientIds.Add((Guid)item.ClientId!);
                    }
                }
            }

            HashSet<Guid> processedGroups = new HashSet<Guid> { groupId };

            await CollectClientIdsFromSubgroups(groupId, allGroups, clientIds, processedGroups);

            return clientIds.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAllClientIdsFromGroupAndSubgroups: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<List<Guid>> GetAllClientIdsFromGroupsAndSubgroupsFromList(List<Guid> groupIds)
    {
        if (groupIds == null || !groupIds.Any())
        {
            return new List<Guid>();
        }

        HashSet<Guid> clientIds = new HashSet<Guid>();

        try
        {
            // Verify all groups exist
            var existingGroups = await context.Group
                .Where(g => groupIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync();

            var missingGroups = groupIds.Except(existingGroups).ToList();
            if (missingGroups.Any())
            {
                throw new KeyNotFoundException($"Groups with IDs {string.Join(", ", missingGroups)} were not found");
            }

            // Get all groups once
            var allGroups = await context.Group
                .AsNoTracking()
                .Include(g => g.GroupItems)
                .ToListAsync();

            // Process each main group
            HashSet<Guid> processedGroups = new HashSet<Guid>();

            foreach (var groupId in groupIds)
            {
                if (processedGroups.Contains(groupId))
                {
                    continue; // Skip if already processed as subgroup
                }

                var mainGroup = allGroups.FirstOrDefault(g => g.Id == groupId);
                if (mainGroup != null)
                {
                    // Add clients from main group
                    if (mainGroup.GroupItems != null)
                    {
                        foreach (var item in mainGroup.GroupItems.Where(x => x.ClientId.HasValue))
                        {
                            if (item != null)
                            {
                                clientIds.Add((Guid)item.ClientId!);
                            }
                        }
                    }

                    processedGroups.Add(groupId);

                    // Collect from subgroups
                    await CollectClientIdsFromSubgroups(groupId, allGroups, clientIds, processedGroups);
                }
            }

            return clientIds.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAllClientIdsFromGroupsAndSubgroups: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task CollectClientIdsFromSubgroups(
       Guid parentId,
       List<Group> allGroups,
       HashSet<Guid> clientIds,
       HashSet<Guid> processedGroups)
    {
        var childGroups = allGroups
            .Where(g => g.Parent == parentId && !processedGroups.Contains(g.Id))
            .ToList();

        foreach (var childGroup in childGroups)
        {
            processedGroups.Add(childGroup.Id);

            if (childGroup.GroupItems != null && childGroup.GroupItems.Any())
            {
                foreach (var item in childGroup.GroupItems.Where(x => x.ClientId.HasValue))
                {
                    if (item != null)
                    {
                        clientIds.Add((Guid)item.ClientId!);
                    }
                }
            }

            await CollectClientIdsFromSubgroups(childGroup.Id, allGroups, clientIds, processedGroups);
        }
    }
}