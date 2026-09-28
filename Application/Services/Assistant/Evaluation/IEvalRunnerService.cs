// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs a goldset against the knowledge retrieval pipeline and persists an EvalRun record.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public interface IEvalRunnerService
{
    Task<EvalRun> RunAsync(string goldset, CancellationToken cancellationToken = default);
}
