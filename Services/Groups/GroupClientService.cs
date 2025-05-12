using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Klacks.Api.Models.Associations;

namespace Klacks.Api.Services.Groups;

/// <summary>
/// Service for retrieving all ClientIds from a group and subgroups
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
                throw new KeyNotFoundException($"Gruppe mit ID {groupId} wurde nicht gefunden");
            }

            var allGroups = await context.Group
                .AsNoTracking()
                .Include(g => g.GroupItems)
                .ToListAsync();

            if (mainGroup.GroupItems != null)
            {
                foreach (var item in mainGroup.GroupItems)
                {
                    clientIds.Add(item.ClientId);
                }
            }

            HashSet<Guid> processedGroups = new HashSet<Guid> { groupId };

            await CollectClientIdsFromSubgroups(groupId, allGroups, clientIds, processedGroups);

            return clientIds.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler in GetAllClientIdsFromGroupAndSubgroups: {ex.Message}");
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
                foreach (var item in childGroup.GroupItems)
                {
                    clientIds.Add(item.ClientId);
                }
            }

            await CollectClientIdsFromSubgroups(childGroup.Id, allGroups, clientIds, processedGroups);
        }
    }
}
