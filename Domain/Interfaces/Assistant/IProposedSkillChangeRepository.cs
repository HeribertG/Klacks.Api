// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for ProposedSkillChange records produced by the Skill-Description-Optimizer (Agent C).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProposedSkillChangeRepository
{
    Task AddAsync(ProposedSkillChange record, CancellationToken cancellationToken = default);

    Task<ProposedSkillChange?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProposedSkillChange record, CancellationToken cancellationToken = default);

    Task<List<ProposedSkillChange>> GetPendingAsync(int limit, CancellationToken cancellationToken = default);

    Task<bool> HasOpenProposalForSkillAsync(Guid skillId, string field, CancellationToken cancellationToken = default);
}
