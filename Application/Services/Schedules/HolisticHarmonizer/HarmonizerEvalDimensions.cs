// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Aggregated dimension values of one Holistic Harmonizer model eval run; serialized into
/// <c>EvalRun.DimensionsJson</c>.
/// </summary>
/// <param name="ParseRate">Fraction of LLM calls that returned parseable batch JSON (0..1).</param>
/// <param name="BatchAcceptanceRate">Accepted (or partially accepted) batches divided by all proposed batches; 0 when nothing was proposed.</param>
/// <param name="NormalizedFitnessImprovement">Mean per-scenario fitness gain normalized by the remaining headroom (0..1).</param>
/// <param name="LlmCallsTotal">Number of ProposeAsync calls issued across all scenarios.</param>
/// <param name="LlmCallsParsed">Number of calls whose response parsed successfully.</param>
/// <param name="BatchesProposed">Total batches the model emitted across all calls.</param>
/// <param name="BatchesAccepted">Batches that survived hard validation, committee and score-greedy.</param>
/// <param name="ScenariosTotal">Number of eval scenarios.</param>
/// <param name="ScenariosWithAcceptedBatch">Scenarios in which at least one batch was accepted.</param>
public sealed record HarmonizerEvalDimensions(
    decimal ParseRate,
    decimal BatchAcceptanceRate,
    decimal NormalizedFitnessImprovement,
    int LlmCallsTotal,
    int LlmCallsParsed,
    int BatchesProposed,
    int BatchesAccepted,
    int ScenariosTotal,
    int ScenariosWithAcceptedBatch);
