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

    /// <summary>
    /// Pending proposals of one field. The field is a parameter and not a filter in the caller, because the
    /// sharpener may only take as many rows as it can decide on: a proposal of another field that it skips
    /// would still consume one of its per-run slots and starve description sharpening for good.
    /// Ordered correction-born first, newest first inside each origin. The goldset branch opens its
    /// proposals in bulk and therefore with the newer timestamps, so a plain newest-first window would push
    /// the proposals a person actually caused out of every run. Callers of another field are unaffected as
    /// long as their rows carry the default origin; a field whose rows mix origins gets a window shaped by
    /// origin before age.
    /// </summary>
    Task<List<ProposedSkillChange>> GetPendingAsync(
        string field, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this skill already carries a proposal the loop must not stack another one on: one still
    /// awaiting a verdict, or one the loop applied by itself. The applied case is what stops the
    /// sharpening from turning its own ratchet - a description narrowed automatically stays as it is until
    /// an administrator has seen it, because the corrections a narrowing produces would otherwise justify
    /// the next narrowing.
    /// </summary>
    Task<bool> HasOpenProposalForSkillAsync(Guid skillId, string field, CancellationToken cancellationToken = default);

    /// <summary>
    /// Proposals of one field in any of the given statuses, newest first. The field is a SQL parameter and
    /// not a filter in the caller for the same reason the limit is: the window is taken newest-first, so a
    /// burst of rows of another field would fill it and leave the caller with nothing after filtering.
    /// </summary>
    Task<List<ProposedSkillChange>> GetByStatusesAsync(
        IReadOnlyList<string> statuses, string field, int limit, CancellationToken cancellationToken = default);

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
