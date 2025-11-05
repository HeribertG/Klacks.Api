using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Extensions;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Groups;

public class GroupMembershipService : IGroupMembershipService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<GroupMembershipService> _logger;
    private readonly IGroupHierarchyService _hierarchyService;

    public GroupMembershipService(DataBaseContext context, ILogger<GroupMembershipService> logger, IGroupHierarchyService hierarchyService)
    {
        _context = context;
        this._logger = logger;
        _hierarchyService = hierarchyService;
    }

    public async Task UpdateGroupMembershipAsync(Guid groupId, IEnumerable<Guid> newClientIds, Guid? shiftId = null)
    {
        _logger.LogInformation("Updating group membership for group {GroupId} with {Count} clients", 
            groupId, newClientIds.Count());

        var existingIds = await _context.GroupItem
            .Where(x => x.GroupId == groupId && x.ClientId.HasValue)
            .Select(x => x.ClientId!.Value)
            .ToHashSetAsync();

        var newIds = newClientIds.ToHashSet();

        // Find items to delete
        var itemsToDelete = await _context.GroupItem
            .Where(x => x.GroupId == groupId &&
                       x.ClientId.HasValue &&
                       !newIds.Contains(x.ClientId.Value))
            .ToListAsync();

        if (itemsToDelete.Any())
        {
            _logger.LogDebug("Removing {Count} clients from group {GroupId}", itemsToDelete.Count, groupId);
            _context.GroupItem.RemoveRange(itemsToDelete);
        }

        // Find IDs to add
        var idsToAdd = newIds.Except(existingIds);
        if (idsToAdd.Any())
        {
            _logger.LogDebug("Adding {Count} clients to group {GroupId}", idsToAdd.Count(), groupId);
            var newItems = idsToAdd.Select(clientId => new GroupItem
            {
                ClientId = clientId,
                GroupId = groupId,
                ShiftId = shiftId
            });

            _context.GroupItem.AddRange(newItems);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Group membership updated for group {GroupId}", groupId);
    }

    public async Task AddClientToGroupAsync(Guid groupId, Guid clientId, Guid? shiftId = null)
    {
        _logger.LogInformation("Adding client {ClientId} to group {GroupId}", clientId, groupId);

        var existingItem = await _context.GroupItem
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.ClientId == clientId);

        if (existingItem != null)
        {
            _logger.LogDebug("Employee {ClientId} is already a member of group {GroupId}", clientId, groupId);
            return;
        }

        var groupItem = new GroupItem
        {
            GroupId = groupId,
            ClientId = clientId,
            ShiftId = shiftId
        };

        _context.GroupItem.Add(groupItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {ClientId} added to group {GroupId}", clientId, groupId);
    }

    public async Task RemoveClientFromGroupAsync(Guid groupId, Guid clientId)
    {
        _logger.LogInformation("Removing client {ClientId} from group {GroupId}", clientId, groupId);

        var groupItem = await _context.GroupItem
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.ClientId == clientId);

        if (groupItem == null)
        {
            _logger.LogDebug("Employee {ClientId} is not a member of group {GroupId}", clientId, groupId);
            return;
        }

        _context.GroupItem.Remove(groupItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {ClientId} removed from group {GroupId}", clientId, groupId);
    }

    public async Task<IEnumerable<Client>> GetGroupMembersAsync(Guid groupId)
    {
        _logger.LogInformation("Getting members for group {GroupId}", groupId);

        var members = await _context.GroupItem
            .Where(gi => gi.GroupId == groupId && gi.ClientId.HasValue)
            .Include(gi => gi.Client)
            .Select(gi => gi.Client!)
            .ToListAsync();

        _logger.LogInformation("Found {Count} members in group {GroupId}", members.Count, groupId);
        return members;
    }

    public async Task<IEnumerable<Group>> GetClientGroupsAsync(Guid clientId)
    {
        _logger.LogInformation("Getting groups for client {ClientId}", clientId);

        var groups = await _context.GroupItem
            .Where(gi => gi.ClientId == clientId)
            .Include(gi => gi.Group)
            .Select(gi => gi.Group)
            .Where(g => g != null)
            .ToListAsync();

        _logger.LogInformation("Found {Count} groups for client {ClientId}", groups.Count, clientId);
        return groups!;
    }

    public async Task<bool> IsClientInGroupAsync(Guid groupId, Guid clientId)
    {
        _logger.LogDebug("Checking if client {ClientId} is in group {GroupId}", clientId, groupId);

        var exists = await _context.GroupItem
            .AnyAsync(gi => gi.GroupId == groupId && gi.ClientId == clientId);

        _logger.LogDebug("Employee {ClientId} membership in group {GroupId}: {IsMember}", clientId, groupId, exists);
        return exists;
    }

    public async Task<int> GetGroupMemberCountAsync(Guid groupId)
    {
        _logger.LogDebug("Getting member count for group {GroupId}", groupId);

        var count = await _context.GroupItem
            .Where(gi => gi.GroupId == groupId && gi.ClientId.HasValue)
            .CountAsync();

        _logger.LogDebug("Group {GroupId} has {Count} members", groupId, count);
        return count;
    }

    public async Task<IEnumerable<Client>> GetGroupHierarchyMembersAsync(Guid groupId)
    {
        _logger.LogInformation("Getting hierarchy members for group {GroupId}", groupId);

        // Get all descendants including the group itself
        var descendants = await _hierarchyService.GetDescendantsAsync(groupId, includeParent: true);
        var descendantIds = descendants.Select(g => g.Id).ToList();

        var hierarchyMembers = await _context.GroupItem
            .Where(gi => descendantIds.Contains(gi.GroupId) && gi.ClientId.HasValue)
            .Include(gi => gi.Client)
            .Select(gi => gi.Client!)
            .Distinct()
            .ToListAsync();

        _logger.LogInformation("Found {Count} members in group hierarchy {GroupId}", hierarchyMembers.Count, groupId);
        return hierarchyMembers;
    }

    public async Task BulkAddClientsToGroupAsync(Guid groupId, IEnumerable<Guid> clientIds, Guid? shiftId = null)
    {
        _logger.LogInformation("Bulk adding {Count} clients to group {GroupId}", clientIds.Count(), groupId);

        var clientIdsList = clientIds.ToList();
        
        // Get existing memberships to avoid duplicates
        var existingIds = await _context.GroupItem
            .Where(gi => gi.GroupId == groupId && gi.ClientId.HasValue && clientIdsList.Contains(gi.ClientId!.Value))
            .Select(gi => gi.ClientId!.Value)
            .ToHashSetAsync();

        var newIds = clientIdsList.Except(existingIds);
        
        if (newIds.Any())
        {
            var newGroupItems = newIds.Select(clientId => new GroupItem
            {
                GroupId = groupId,
                ClientId = clientId,
                ShiftId = shiftId
            });

            _context.GroupItem.AddRange(newGroupItems);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk added {Count} new clients to group {GroupId}", newIds.Count(), groupId);
        }
        else
        {
            _logger.LogDebug("No new clients to add to group {GroupId} - all were already members", groupId);
        }
    }

    public async Task BulkRemoveClientsFromGroupAsync(Guid groupId, IEnumerable<Guid> clientIds)
    {
        _logger.LogInformation("Bulk removing {Count} clients from group {GroupId}", clientIds.Count(), groupId);

        var clientIdsList = clientIds.ToList();
        
        var itemsToRemove = await _context.GroupItem
            .Where(gi => gi.GroupId == groupId && gi.ClientId.HasValue && clientIdsList.Contains(gi.ClientId.Value))
            .ToListAsync();

        if (itemsToRemove.Any())
        {
            _context.GroupItem.RemoveRange(itemsToRemove);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk removed {Count} clients from group {GroupId}", itemsToRemove.Count, groupId);
        }
        else
        {
            _logger.LogDebug("No clients to remove from group {GroupId} - none were members", groupId);
        }
    }
}