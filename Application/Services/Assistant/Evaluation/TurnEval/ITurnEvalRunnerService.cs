// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs a turn-selection goldset against one model, scores the results and persists an
/// EvalRun aggregate with per-model baseline regression.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public interface ITurnEvalRunnerService
{
    Task<TurnEvalRunResult> RunAsync(
        string goldset,
        string modelId,
        int? maxItems,
        string userId,
        List<string> userRights,
        CancellationToken cancellationToken = default);

    Task<TurnEvalRunResult> RunAsync(
        string goldset,
        IReadOnlyList<TurnGoldsetItem> items,
        string modelId,
        int? maxItems,
        string userId,
        List<string> userRights,
        CancellationToken cancellationToken = default);
}
