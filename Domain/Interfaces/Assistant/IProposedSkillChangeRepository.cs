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

    /// <summary>
    /// Whether this skill already carries a proposal the loop must not stack another one on: one still
    /// awaiting a verdict, or one the loop applied by itself. The applied case is what stops the
    /// sharpening from turning its own ratchet - a description narrowed automatically stays as it is until
    /// an administrator has seen it, because the corrections a narrowing produces would otherwise justify
    /// the next narrowing.
    /// </summary>
    Task<bool> HasOpenProposalForSkillAsync(Guid skillId, string field, CancellationToken cancellationToken = default);

    Task<List<ProposedSkillChange>> GetByStatusesAsync(
        IReadOnlyList<string> statuses, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many proposals were moved into each of the given statuses inside a half-open window, the
    /// question the weekly digest asks about automatically applied and regression-blocked sharpenings.
    /// Counted on ReviewedAt, the moment the decision was taken.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> CountByStatusInWindowAsync(
        IReadOnlyList<string> statuses,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
