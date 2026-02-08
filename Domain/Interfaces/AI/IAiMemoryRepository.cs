using Klacks.Api.Domain.Models.AI;

namespace Klacks.Api.Domain.Interfaces.AI;

public interface IAiMemoryRepository
{
    Task<List<AiMemory>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<AiMemory>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    Task<AiMemory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<AiMemory>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    Task AddAsync(AiMemory memory, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiMemory memory, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
