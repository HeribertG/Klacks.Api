// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRelationRepository
{
    Task<List<SkillRelation>> GetByAgentAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<SkillRelation> relations, CancellationToken cancellationToken = default);
    Task UpdateAsync(SkillRelation relation, CancellationToken cancellationToken = default);
}
