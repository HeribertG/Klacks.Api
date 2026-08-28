// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for persisting skill selection trajectories captured during chat turns.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillSelectionTrajectoryRepository
{
    Task AddAsync(SkillSelectionTrajectory record, CancellationToken cancellationToken = default);

    Task<SkillSelectionTrajectory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(SkillSelectionTrajectory record, CancellationToken cancellationToken = default);

    Task<List<SkillSelectionTrajectory>> GetRecentAsync(Guid agentId, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wrong-skill trajectories the description optimizer has not consumed yet, newest first. Evidence a
    /// proposal was already built from is excluded, so a sharpening cannot keep re-proposing itself from
    /// the same handful of corrections on every run. Filtered to CorrectionTypes.WrongSkill in the query
    /// itself, so an implicit correction can never occupy a slot in this window without ever being stamped.
    /// </summary>
    Task<List<SkillSelectionTrajectory>> GetUncorrectedWrongSkillAsync(Guid agentId, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps the consumption watermark on the given trajectories. Written as one conditional statement so
    /// two overlapping runs cannot both claim the same evidence.
    /// </summary>
    Task MarkSharpenedAsync(
        IReadOnlyList<Guid> ids, DateTime sharpenedAtUtc, CancellationToken cancellationToken = default);

    Task<SkillSelectionTrajectory?> FindMostRecentByUserAndHashAsync(string userId, string userMessageHash, CancellationToken cancellationToken = default);

    Task<SkillSelectionTrajectory?> FindMostRecentByAgentAndUserAsync(Guid agentId, string userId, CancellationToken cancellationToken = default);
}
