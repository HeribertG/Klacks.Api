using Klacks.Api.Models.Associations;
using Klacks.Api.Resources.Filter;

namespace Klacks.Api.Interfaces;

public interface IGroupRepository : IBaseRepository<Group>
{
  new Task<Group?> Get(Guid id);

  new void Put(Group model);

  Task<TruncatedGroup> Truncated(GroupFilter filter);
}
