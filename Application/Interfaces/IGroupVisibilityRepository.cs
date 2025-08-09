using Klacks.Api.Models.Associations;

namespace Klacks.Api.Application.Interfaces;

public interface IGroupVisibilityRepository : IBaseRepository<GroupVisibility>
{
    Task<IEnumerable<GroupVisibility>> GroupVisibilityList(string id);

    Task<IEnumerable<GroupVisibility>> GetGroupVisibilityList();

    Task SetGroupVisibilityList(List<GroupVisibility> list);
}
