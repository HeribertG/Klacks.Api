using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Interfaces;

public interface IBranchRepository : IBaseRepository<Branch>
{
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);
}
