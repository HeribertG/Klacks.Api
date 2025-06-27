using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories;

public class GroupVisibilityRepository : BaseRepository<GroupVisibility>, IGroupVisibilityRepository
{
    private readonly DataBaseContext context;

    public GroupVisibilityRepository(DataBaseContext context)
      : base(context)
    {
        this.context = context;
    }

    public async Task<List<GroupVisibility>> GroupVisibilityList(string id)
    {
        return await context.GroupVisibility.Where(x=> x.AppUserId == id).ToListAsync();
    }
}
