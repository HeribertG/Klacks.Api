using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftGroupManagementService : IShiftGroupManagementService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<ShiftGroupManagementService> _logger;

    public ShiftGroupManagementService(DataBaseContext context, ILogger<ShiftGroupManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task UpdateGroupItemsAsync(Guid shiftId, List<Guid> actualGroupIds)
    {
        _logger.LogInformation("Updating group items for shift ID: {ShiftId}", shiftId);
        
        try
        {
            var existingIds = await _context.GroupItem
                .Where(gi => gi.ShiftId == shiftId)
                .Select(x => x.GroupId)
                .ToListAsync();

            var newGroupIds = actualGroupIds.Where(x => !existingIds.Contains(x)).ToArray();
            var deleteGroupItems = await _context.GroupItem
                .Where(gi => gi.ShiftId == shiftId && !actualGroupIds.Contains(gi.GroupId))
                .ToArrayAsync();
            var newGroupItems = newGroupIds.Select(x => new GroupItem { ShiftId = shiftId, GroupId = x }).ToArray();

            if (deleteGroupItems.Any())
            {
                _context.GroupItem.RemoveRange(deleteGroupItems);
            }

            if (newGroupItems.Any())
            {
                _context.GroupItem.AddRange(newGroupItems);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Group items for shift ID: {ShiftId} updated successfully.", shiftId);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update group items for shift ID: {ShiftId}.", shiftId);
            throw new DbUpdateException($"Failed to update group items for shift ID: {shiftId}.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating group items for shift ID: {ShiftId}.", shiftId);
            throw;
        }
    }

    public async Task<List<Group>> GetGroupsForShiftAsync(Guid shiftId)
    {
        return await _context.Group
            .Include(g => g.GroupItems.Where(gi => gi.ShiftId == shiftId))
            .Where(g => g.GroupItems.Any(gi => gi.ShiftId == shiftId))
            .AsNoTracking()
            .ToListAsync();
    }
}