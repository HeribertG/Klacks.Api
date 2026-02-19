using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentRepository
{
    Task<Agent?> GetDefaultAgentAsync(CancellationToken cancellationToken = default);
    Task<Agent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Agent>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Agent agent, CancellationToken cancellationToken = default);
    Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default);
}
