// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Replays a single goldset turn headlessly against a specific model: assembles the
/// production toolset and system prompt, performs exactly one provider call and stops
/// after the model's tool choice, without executing tools or writing telemetry.
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
}
