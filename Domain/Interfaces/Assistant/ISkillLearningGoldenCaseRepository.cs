// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the frozen routing expectations the loop builds up. Self-committing.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningGoldenCaseRepository
{
    Task AddAsync(SkillLearningGoldenCase goldenCase, CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyList<SkillLearningGoldenCase> goldenCases, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes back cases that were read from this repository and changed since. Used by the goldset seeder
    /// when the shipped file corrects an expectation an earlier startup already imported.
    /// The cases have to be the detached instances the read returned. A row this scope inserted itself is
    /// already tracked, and attaching a second instance of it throws - the seeder never does that, because
    /// a key it just inserted is by construction not a key it also corrects.
    /// </summary>
    Task UpdateRangeAsync(
        IReadOnlyList<SkillLearningGoldenCase> goldenCases, CancellationToken cancellationToken = default);

    /// <summary>
    /// The holdout half - the only population a gate is allowed to measure. Train cases feed learning,
    /// so replaying them would let the loop be judged on the very cases it optimised against.
    /// The budget is finite, so it is spent where a change is most likely to break something: first the
    /// cases of the skill under change, then the remaining shipped goldset cases in a stable order, then
    /// the newest learned ones. The goldset cases are seeded once and are therefore permanently the oldest
    /// rows, so a plain newest-first window would displace the curated population the gate exists for, and
    /// an order by age would let a newly learned case of the same skill reshuffle what the gate measures.
    /// </summary>
    /// <param name="limit">Upper bound on the replayed cases, goldset cases included</param>
    /// <param name="prioritisedSkillName">Skill whose cases are replayed first, null when the caller has none</param>
    Task<IReadOnlyList<SkillLearningGoldenCase>> ListHoldoutAsync(
        int limit, string? prioritisedSkillName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Size of the holdout half, asked before a gate decides anything: below the configured minimum a
    /// green gate means "nothing was measured", not "nothing broke".
    /// </summary>
    Task<int> CountHoldoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Every case of one origin, used by the goldset seeder to build its idempotency key set in one
    /// query instead of one existence check per item.
    /// </summary>
    Task<IReadOnlyList<SkillLearningGoldenCase>> ListByOriginAsync(
        string origin, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string query, string expectedSourceId, CancellationToken cancellationToken = default);
}
