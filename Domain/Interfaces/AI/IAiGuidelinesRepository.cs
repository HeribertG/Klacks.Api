using Klacks.Api.Domain.Models.AI;

namespace Klacks.Api.Domain.Interfaces.AI;

public interface IAiGuidelinesRepository
{
    Task<AiGuidelines?> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<List<AiGuidelines>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AiGuidelines?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(AiGuidelines guidelines, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiGuidelines guidelines, CancellationToken cancellationToken = default);

    Task DeactivateAllAsync(CancellationToken cancellationToken = default);
}
