// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Runs the fixed Holistic Harmonizer eval scenarios against one LLM model and persists
/// the aggregated result as an EvalRun under the <see cref="HarmonizerEvalGoldset.Name"/> goldset.
/// </summary>
public interface IHarmonizerEvalRunnerService
{
    Task<HarmonizerEvalRunResult> RunAsync(string modelId, CancellationToken cancellationToken = default);
}
