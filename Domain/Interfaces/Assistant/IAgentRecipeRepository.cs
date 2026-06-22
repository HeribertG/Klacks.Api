// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentRecipeRepository
{
    Task<List<AgentRecipe>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task<List<AgentRecipe>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AgentRecipe?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(AgentRecipe recipe, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentRecipe recipe, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
