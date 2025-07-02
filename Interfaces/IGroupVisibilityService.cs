using Klacks.Api.Models.Associations;

namespace Klacks.Api.Interfaces
{
    public interface IGroupVisibilityService
    {
        Task<List<Guid>> ReadVisibleRootIdList();

        Task<List<GroupVisibility>> ReviseAdminVisibility(List<GroupVisibility> list);

        Task<List<string>> ReadAdmins();

        Task<bool> IsAdmin();
    }
}
