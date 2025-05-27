using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;

namespace Klacks.Api.Repositories;

public class GroupVisibilityRepository : BaseRepository<GroupVisibility>, IGroupVisibilityRepository
{
    public GroupVisibilityRepository(DataBaseContext context) : base(context)
    {
    }
}
