// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the generated candidates of a learning cluster. Self-committing, like the other
/// learning repositories: its callers are background runs, and each verdict must survive the next one.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningCandidateRepository
{
    Task AddAsync(SkillLearningCandidate candidate, CancellationToken cancellationToken = default);

    Task UpdateVerdictAsync(
        Guid id,
        string status,
        string? routingResultJson,
        string? errorText,
        DateTime? activatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<int> CountByClusterAsync(Guid clusterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillLearningCandidate>> ListByClusterAsync(
        Guid clusterId, CancellationToken cancellationToken = default);
}
