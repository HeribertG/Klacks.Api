// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the frozen routing expectations the loop builds up. Self-committing.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningGoldenCaseRepository
{
    Task AddAsync(SkillLearningGoldenCase goldenCase, CancellationToken cancellationToken = default);

    /// <summary>
    /// The goldset the regression gate replays, newest first and capped: every case costs one embedding
    /// and one reranking pass, so an unbounded set would make each learning round scale with history.
    /// </summary>
    Task<IReadOnlyList<SkillLearningGoldenCase>> ListAsync(int limit, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string query, string expectedSourceId, CancellationToken cancellationToken = default);
}
