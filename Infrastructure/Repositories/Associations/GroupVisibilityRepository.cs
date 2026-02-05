using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class GroupVisibilityRepository : BaseRepository<GroupVisibility>, IGroupVisibilityRepository
{
    private readonly DataBaseContext context;
    private readonly IGroupVisibilityService groupVisibility;

    public GroupVisibilityRepository(DataBaseContext context, IGroupVisibilityService groupVisibility, ILogger<GroupVisibility> logger)
      : base(context, logger)
    {
        this.context = context;
        this.groupVisibility = groupVisibility;
    }

    public async Task<IEnumerable<GroupVisibility>> GroupVisibilityList(string id)
    {
        var list = await context.GroupVisibility.AsNoTracking().Where(x => x.AppUserId == id).ToListAsync();
        list = await groupVisibility.ReviseAdminVisibility(list);

        return list;
    }

    public async Task<IEnumerable<GroupVisibility>> GetGroupVisibilityList()
    {
        var list = await context.GroupVisibility.AsNoTracking().ToListAsync();
        list = await groupVisibility.ReviseAdminVisibility(list);

        return list;
    }

    public async Task SetGroupVisibilityList(List<GroupVisibility> list)
    {
        Logger.LogInformation("Setting group visibility list with {Count} entries.", list.Count);
        try
        {
            var existingGroupVisibilities = await context.GroupVisibility.ToListAsync();
            context.GroupVisibility.RemoveRange(existingGroupVisibilities);

            context.GroupVisibility.AddRange(list);
            await context.SaveChangesAsync();
            Logger.LogInformation("Group visibility list updated successfully.");
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to set group visibility list. Database update error.");
            throw new InvalidRequestException("Failed to set group visibility list due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while setting group visibility list.");
            throw;
        }
    }
}
