using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

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
}
