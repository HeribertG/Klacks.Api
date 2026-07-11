// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Per-scenario outcome of one Holistic Harmonizer model eval run.
/// </summary>
/// <param name="Name">Scenario label from the factory.</param>
/// <param name="FitnessBefore">Harmony fitness of the untouched scenario plan (0..1).</param>
/// <param name="FitnessAfter">Harmony fitness after all accepted batches were applied (0..1).</param>
/// <param name="LlmCallsTotal">ProposeAsync calls issued for this scenario.</param>
/// <param name="LlmCallsParsed">Calls whose response parsed successfully.</param>
/// <param name="BatchesProposed">Batches the model emitted for this scenario.</param>
/// <param name="BatchesAccepted">Batches accepted by the production acceptance pipeline.</param>
/// <param name="LastError">Most recent parsing/provider error; null when every call parsed.</param>
public sealed record HarmonizerEvalScenarioResult(
    string Name,
    double FitnessBefore,
    double FitnessAfter,
    int LlmCallsTotal,
    int LlmCallsParsed,
    int BatchesProposed,
    int BatchesAccepted,
    string? LastError);
