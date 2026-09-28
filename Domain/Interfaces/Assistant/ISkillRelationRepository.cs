// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRelationRepository
{
    Task<List<SkillRelation>> GetByAgentAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<List<SkillRelation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SkillRelation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<SkillRelation> relations, CancellationToken cancellationToken = default);
    Task UpdateAsync(SkillRelation relation, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<SkillRelation> relations, CancellationToken cancellationToken = default);
}
