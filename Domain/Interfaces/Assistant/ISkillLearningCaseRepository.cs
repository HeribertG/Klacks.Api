// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the individual occurrences behind a learning cluster. Self-committing, for the same
/// reason as ISkillLearningClusterRepository.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningCaseRepository
{
    Task AddAsync(SkillLearningCase learningCase, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many different users produced a case in this cluster. Counted as SQL COUNT(DISTINCT user_id),
    /// so cases without a user id contribute nothing at all - a path that cannot name its user can never
    /// push a cluster over the "several different people asked for this" threshold.
    /// </summary>
    Task<int> CountDistinctUsersAsync(Guid clusterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurrences per signal kind for one cluster, the source of the stored signal histogram.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> CountBySignalAsync(Guid clusterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillLearningCase>> ListByClusterAsync(Guid clusterId, int limit, CancellationToken cancellationToken = default);
}
