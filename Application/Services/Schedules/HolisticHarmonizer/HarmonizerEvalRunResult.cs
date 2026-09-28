// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Full result of one Holistic Harmonizer model eval run.
/// </summary>
/// <param name="Run">The persisted EvalRun row (goldset, composite, regression, counters).</param>
/// <param name="Dimensions">Aggregated dimension values mirrored into <c>Run.DimensionsJson</c>.</param>
/// <param name="Scenarios">Per-scenario details for reporting and diagnostics.</param>
public sealed record HarmonizerEvalRunResult(
    EvalRun Run,
    HarmonizerEvalDimensions Dimensions,
    IReadOnlyList<HarmonizerEvalScenarioResult> Scenarios);
