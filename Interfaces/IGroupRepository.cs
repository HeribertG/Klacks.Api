using Klacks_api.Models.Associations;
using Klacks_api.Resources.Filter;

namespace Klacks_api.Interfaces;

public interface IGroupRepository : IBaseRepository<Group>
{
  new Task<Group?> Get(Guid id);

  new void Put(Group model);

  Task<TruncatedGroup> Truncated(GroupFilter filter);
}
