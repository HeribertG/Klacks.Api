using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IAssignedGroupRepository : IBaseRepository<AssignedGroup>
{
    Task<IEnumerable<Group>> Assigned(Guid? id);
}
