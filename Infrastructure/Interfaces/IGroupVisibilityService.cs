using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Infrastructure.Interfaces
{
    public interface IGroupVisibilityService
    {
        Task<List<Guid>> ReadVisibleRootIdList();

        Task<List<GroupVisibility>> ReviseAdminVisibility(List<GroupVisibility> list);

        Task<List<string>> ReadAdmins();

        Task<bool> IsAdmin();
    }
}
