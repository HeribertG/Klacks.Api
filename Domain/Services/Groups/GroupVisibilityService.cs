using Klacks.Api.Domain.Constants;
using Klacks.Api.Datas;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Authentification;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Groups
{
    public class GroupVisibilityService : IGroupVisibilityService
    {
        private readonly DataBaseContext context;
        private readonly UserManager<AppUser> userManager;
        private readonly IUserService user;

        public async Task<bool> IsAdmin()
        {
            return await user.IsAdmin();
        }

        public GroupVisibilityService(DataBaseContext context, UserManager<AppUser> userManager, IUserService user)
        {
            this.context = context;
            this.userManager = userManager;
            this.user = user;
        }

        public async Task<List<Guid>> ReadVisibleRootIdList()
        {
            List<Guid> list = [];
            var isAdmin = await IsAdmin();
            var userIdString = user.GetIdString();
            if (!isAdmin && !string.IsNullOrEmpty(user.GetIdString()))
            {
                var userId = user.GetIdString()!;
                return await context.GroupVisibility.Where(x => x.AppUserId == userId!).Select(x => x.GroupId).ToListAsync();
            }

            return list;
        }

        public async Task<List<GroupVisibility>> ReviseAdminVisibility(List<GroupVisibility> list)
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

        public async Task<List<string>> ReadAdmins()
        {
            List<string> list = [];
            var users = await context.AppUser.ToListAsync();
            var usersInAdminRole = await userManager.GetUsersInRoleAsync(Roles.Admin);

            foreach (var user in users)
            {
                if (usersInAdminRole.Contains(user))
                {
                    list.Add(user.Id);
                }
            }

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
    }
}
