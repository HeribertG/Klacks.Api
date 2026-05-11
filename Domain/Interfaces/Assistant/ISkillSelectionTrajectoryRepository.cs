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

    Task<List<SkillSelectionTrajectory>> GetCorrectedAsync(Guid agentId, int limit, CancellationToken cancellationToken = default);

    Task<SkillSelectionTrajectory?> FindMostRecentByUserAndHashAsync(string userId, string userMessageHash, CancellationToken cancellationToken = default);
}
