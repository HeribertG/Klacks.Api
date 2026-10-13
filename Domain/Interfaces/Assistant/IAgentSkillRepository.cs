// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentSkillRepository
{
    Task<List<AgentSkill>> GetAllEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracking twin of <see cref="GetAllEnabledAsync"/>. Use when a caller shares its scope with
    /// another installer/loader that already attached the same rows (e.g. language plugin install) -
    /// a second no-tracking read would hand Update() a second instance of an already-tracked key,
    /// which EF rejects as an identity conflict.
    /// </summary>
    Task<List<AgentSkill>> GetAllEnabledTrackedAsync(CancellationToken cancellationToken = default);
    Task<List<AgentSkill>> GetEnabledAsync(Guid agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The RequiredPermission of the named enabled skills, a name-only projection so the recipe rights check does not
    /// read whole skill rows. A skill that is not enabled or does not exist is absent from the result; a skill without a
    /// requirement maps to null or an empty string. Names are matched case-insensitively, in the database as well as in
    /// the returned dictionary. When several agents carry a skill of the same name, the default agent's row decides, then
    /// the lowest sort order, so the answer does not depend on row order.
    /// </summary>
    /// <param name="skillNames">Names of the skills whose requirement is wanted</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyDictionary<string, string?>> GetRequiredPermissionsByNameAsync(
        IReadOnlyCollection<string> skillNames, CancellationToken cancellationToken = default);
    Task<List<AgentSkill>> GetAllByAgentIdAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<AgentSkill?> GetByNameAsync(Guid agentId, string name, CancellationToken cancellationToken = default);
    Task<AgentSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AgentSkill skill, CancellationToken cancellationToken = default);
    Task UpdateAsync(AgentSkill skill, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task LogExecutionAsync(AgentSkillExecution execution, CancellationToken cancellationToken = default);
    Task<int> GetSessionCallCountAsync(Guid skillId, Guid sessionId, CancellationToken cancellationToken = default);
}
