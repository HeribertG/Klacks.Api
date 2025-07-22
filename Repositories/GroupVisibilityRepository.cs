using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories;

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
        var existingGroupVisibilities = await context.GroupVisibility.ToListAsync();
        context.GroupVisibility.RemoveRange(existingGroupVisibilities);

        context.GroupVisibility.AddRange(list);
    }
}
