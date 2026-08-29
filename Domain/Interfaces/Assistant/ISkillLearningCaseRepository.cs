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

    /// <summary>
    /// The skill the user named when correcting the turn behind this trajectory, or null when the
    /// correction named none. The optimizer freezes that skill as the golden target of the excerpt it is
    /// about to sharpen a description against.
    /// </summary>
    Task<string?> FindExpectedSkillByTrajectoryAsync(Guid trajectoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this user already produced a case in this cluster inside the given window. The refusal
    /// path and the implicit-correction path observe the same unhappy exchange from two sides, so without
    /// this check a single failed turn would count twice towards the repetition threshold.
    /// Cases without a user id are deduplicated against each other as well: they are indistinguishable,
    /// and they never contribute to the distinct-user threshold anyway.
    /// </summary>
    Task<bool> HasCaseSinceAsync(
        Guid clusterId, string? userId, DateTime sinceUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many further occurrences a cluster collected after the given moment. Read after an artefact
    /// was activated, where every new case means the same wish went unserved again - the one negative
    /// signal that does not depend on anybody complaining.
    /// </summary>
    Task<int> CountSinceAsync(Guid clusterId, DateTime sinceUtc, CancellationToken cancellationToken = default);
}
