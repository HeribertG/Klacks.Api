using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class GroupItemRepository : BaseRepository<GroupItem>, IGroupItemRepository
{
    private readonly DataBaseContext context;

    public GroupItemRepository(DataBaseContext context, ILogger<GroupItem> logger)
        : base(context, logger)
    {
        this.context = context;
    }

    public async Task<GroupItem?> GetByClientAndGroup(Guid clientId, Guid groupId)
    {
        return await context.GroupItem
            .FirstOrDefaultAsync(gi => gi.ClientId == clientId && gi.GroupId == groupId);
    }

    public IQueryable<GroupItem> GetQuery()
    {
        return context.GroupItem.AsQueryable();
    }

    public async Task<List<Guid>> GetGroupIdsByShiftId(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ShiftId == shiftId)
            .Select(gi => gi.GroupId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetShiftIdsByGroupIds(List<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
            .Select(gi => gi.ShiftId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetShiftCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ShiftId.HasValue)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetCustomerCountsPerGroupAsync(CancellationToken cancellationToken = default)
    {
        return await context.GroupItem
            .Where(gi => gi.ClientId.HasValue && gi.Client != null && gi.Client.Type == EntityTypeEnum.Customer)
            .GroupBy(gi => gi.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }
}
