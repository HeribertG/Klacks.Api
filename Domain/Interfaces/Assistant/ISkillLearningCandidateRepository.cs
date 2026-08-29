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

    /// <summary>
    /// Records what the oracles decided about one variant.
    /// </summary>
    /// <param name="routingResultJson">Verdict of oracle O1, null when it was not reached</param>
    /// <param name="executionResultJson">Verdict of oracle O2, null for anything but a capability</param>
    Task UpdateVerdictAsync(
        Guid id,
        string status,
        string? routingResultJson,
        string? executionResultJson,
        string? errorText,
        DateTime? activatedAtUtc,
        CancellationToken cancellationToken = default);

    Task<int> CountByClusterAsync(Guid clusterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillLearningCandidate>> ListByClusterAsync(
        Guid clusterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Candidates in one status, newest activation first. The fitness service and the pruner walk the
    /// active ones with it; nothing else needs a cross-cluster view.
    /// </summary>
    Task<IReadOnlyList<SkillLearningCandidate>> ListByStatusAsync(
        string status, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a candidate to retired and records why, without touching its payload or verdicts - the
    /// pruner needs the evidence to survive the retirement it caused.
    /// </summary>
    Task RetireAsync(Guid id, string reason, CancellationToken cancellationToken = default);
}
