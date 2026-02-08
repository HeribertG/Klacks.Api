using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Application.Interfaces;

public interface IClientRepository : IBaseRepository<Client>
{
    Task<int> CountAsync();
    Task<LastChangeMetaData> LastChangeMetaData();
    Task<List<Client>> GetActiveClientsWithAddressesAsync(CancellationToken cancellationToken = default);
    Task<List<Client>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<Client?> GetByLdapExternalIdAsync(string ldapExternalId);
    Task<Client?> GetWithMembershipAsync(Guid clientId, CancellationToken cancellationToken = default);
}
