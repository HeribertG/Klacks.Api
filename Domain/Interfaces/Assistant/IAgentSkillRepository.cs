// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentSkillRepository
{
    Task<List<AgentSkill>> GetEnabledAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<List<AgentSkill>> GetAllByAgentIdAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<AgentSkill?> GetByNameAsync(Guid agentId, string name, CancellationToken cancellationToken = default);
    Task<AgentSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AgentSkill skill, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentSkill skill, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task LogExecutionAsync(AgentSkillExecution execution, CancellationToken cancellationToken = default);
    Task<int> GetSessionCallCountAsync(Guid skillId, Guid sessionId, CancellationToken cancellationToken = default);
}
