using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILlmFunctionDefinitionRepository
{
    Task<List<LlmFunctionDefinition>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task<LlmFunctionDefinition?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<LlmFunctionDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(LlmFunctionDefinition definition, CancellationToken cancellationToken = default);
    Task UpdateAsync(LlmFunctionDefinition definition, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
