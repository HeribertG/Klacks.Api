// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Correlates persisted telemetry (skill usage records, conversation messages and
/// corrected trajectories) into raw turn goldset candidates for human curation.
/// </summary>

using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface ITurnGoldsetCandidateRepository
{
    Task<IReadOnlyList<TurnGoldsetCandidate>> GetCandidatesAsync(
        DateTime fromDate,
        int limit,
        CancellationToken cancellationToken = default);
}
