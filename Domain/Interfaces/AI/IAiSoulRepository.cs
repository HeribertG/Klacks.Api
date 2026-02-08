using Klacks.Api.Domain.Models.AI;

namespace Klacks.Api.Domain.Interfaces.AI;

public interface IAiSoulRepository
{
    Task<AiSoul?> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<List<AiSoul>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AiSoul?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(AiSoul soul, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiSoul soul, CancellationToken cancellationToken = default);

    Task DeactivateAllAsync(CancellationToken cancellationToken = default);
}
