using Klacks.Api.Constants;
using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Authentification;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories;

public class GroupVisibilityRepository : BaseRepository<GroupVisibility>, IGroupVisibilityRepository
{
    private readonly DataBaseContext context;
    private readonly UserManager<AppUser> userManager;

    public GroupVisibilityRepository(DataBaseContext context, UserManager<AppUser> userManager)
      : base(context)
    {
        this.context = context;
        this.userManager = userManager;
    }

    public async Task<IEnumerable<GroupVisibility>> GroupVisibilityList(string id)
    {
        var list = await context.GroupVisibility.AsNoTracking().Where(x => x.AppUserId == id).ToListAsync();
        list = await ReviseAdminVisibility(list);

        return list;
    }

    public async Task<IEnumerable<GroupVisibility>> GetGroupVisibilityList()
    {
        var list = await context.GroupVisibility.AsNoTracking().ToListAsync();
        list = await ReviseAdminVisibility(list);

        return list;
    }

    public async Task SetGroupVisibilityList(List<GroupVisibility> list)
    {
        var existingGroupVisibilities = await context.GroupVisibility.ToListAsync();
        context.GroupVisibility.RemoveRange(existingGroupVisibilities);

        context.GroupVisibility.AddRange(list);
    }

    private async Task<List<GroupVisibility>> ReviseAdminVisibility(List<GroupVisibility> list)
    {
        var filteredUserIds = list.Select(x => x.AppUserId).Distinct();

        var userRoles = new Dictionary<string, IList<string>>();
        foreach (var userId in filteredUserIds)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var roles = await userManager.GetRolesAsync(user);
                if (roles.Contains(Roles.Admin))
                {
                    list = RemoveCurrentUserIfAdmin(userId, list);
                }
            }
        }

        list = await AddAdmins(list);
        return list;
    }

    private List<GroupVisibility> RemoveCurrentUserIfAdmin(string userId, List<GroupVisibility> list)
    {
        return list.Where(x => x.AppUserId != userId).ToList();
    }

    private async Task<List<GroupVisibility>> AddAdmins(List<GroupVisibility> list)
    {
        var roots = await ReadRoots();
        var userIds = await ReadAdmins();

        foreach (var userId in userIds)
        {
            var newGroupVisibilityItems = roots.Select(rootGuid => new GroupVisibility
            {
                Id = Guid.NewGuid(),
                AppUserId = userId,
                GroupId = rootGuid
            }).ToList();

            list.AddRange(newGroupVisibilityItems);
        }

        return list;
    }

    private async Task<List<Guid>> ReadRoots()
    {
        return await context.Group
       .AsNoTracking()
       .Where(g => g.Id == g.Root || !(g.Root.HasValue && g.Parent.HasValue))
       .Select(x => x.Id)
       .ToListAsync();
    }

    private async Task<List<string>> ReadAdmins()
    {
        List<string> list = [];
        var users = await context.AppUser.ToListAsync();
        var usersInAdminRole = await userManager.GetUsersInRoleAsync(Roles.Admin);

        foreach (var user in users)
        {
            if(usersInAdminRole.Contains(user))
            {
                list.Add(user.Id);
            }
        }

        return list;
    }
}
