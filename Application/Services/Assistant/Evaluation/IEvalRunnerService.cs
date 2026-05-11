// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs a goldset against the knowledge retrieval pipeline and persists an EvalRun record.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public interface IEvalRunnerService
{
    Task<EvalRun> RunAsync(string goldset, CancellationToken cancellationToken = default);
}
