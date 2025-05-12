using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories;

public class AssignedGroupRepository : BaseRepository<AssignedGroup>, IAssignedGroupRepository
{
    private readonly DataBaseContext context;
    private readonly IUserService userService;
    public AssignedGroupRepository(DataBaseContext context, IUserService userService) 
        : base(context)
    {
        this.context = context;
        this.userService = userService;
    }

    public async Task<IEnumerable<Group>> Assigned(Guid? id)
    {
        var currentId = id;
        if(currentId==null)
        {
            currentId = userService.GetId();
        }

        var List = await context.AssignedGroup.Where(x => x.ClientId == currentId).Select(x=> x.GroupId).ToListAsync();
        return context.Group.Where(x => (x.Id == x.Root || !(x.Root.HasValue && x.Parent.HasValue)) && List.Contains(x.Id)).AsQueryable();
    }

    
}
