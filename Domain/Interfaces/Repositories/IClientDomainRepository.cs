using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Repositories;

public interface IClientDomainRepository
{
    Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<ClientSummary>> SearchAsync(ClientSearchCriteria criteria, CancellationToken cancellationToken = default);
    
    Task<List<ClientSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<Client> AddAsync(Client client, CancellationToken cancellationToken = default);
    
    Task<Client> UpdateAsync(Client client, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    
    // Domain-specific methods
    Task<List<ClientSummary>> GetActiveClientsAsync(CancellationToken cancellationToken = default);
    
    Task<List<ClientSummary>> GetFormerClientsAsync(CancellationToken cancellationToken = default);
    
    Task<List<ClientSummary>> GetFutureClientsAsync(CancellationToken cancellationToken = default);
    
    Task<List<ClientSummary>> GetByGroupIdAsync(int groupId, bool includeSubGroups = false, CancellationToken cancellationToken = default);
    
    Task<List<Client>> GetByIdNumbersAsync(List<string> idNumbers, CancellationToken cancellationToken = default);
    
    Task<List<Client>> GetByGenderAsync(GenderEnum gender, CancellationToken cancellationToken = default);
    
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    
    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);
}