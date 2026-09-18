// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Replays a single goldset turn headlessly against a specific model: assembles the
/// production toolset and system prompt. <c>ReplayAsync</c> performs exactly one provider
/// call; <c>ReplayWithLookupFollowUpAsync</c> may perform a second one on a synthetic
/// lookup result. Neither executes tools or writes telemetry.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public interface ITurnReplayService
{
    Task<TurnReplayResult> ReplayAsync(
        TurnGoldsetItem item,
        string modelId,
        string userId,
        List<string> userRights,
        CancellationToken cancellationToken = default);

    Task<TurnReplayResult> ReplayWithLookupFollowUpAsync(
        TurnGoldsetItem item,
        string modelId,
        string userId,
        List<string> userRights,
        CancellationToken cancellationToken = default);
}
