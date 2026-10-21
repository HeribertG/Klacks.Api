// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Diagnostics;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Bitmap;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Search;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Mutable state of one inner LLM-ALNS loop of <see cref="HolisticHarmonizerEngine"/>. Created once per run after the
/// pre-flight passed, so reject memory, batch cap, intent statistics, the unseeded intent roulette, counters and the
/// stopwatch never leak between runs.
/// </summary>
/// <param name="request">The run request (model id, language)</param>
/// <param name="working">The working bitmap the accepted batches are applied to</param>
/// <param name="components">Batch evaluator and candidate pool of the run</param>
/// <param name="agentSummary">Per-row agent constraints rendered once for every proposal prompt</param>
/// <param name="fitnessBefore">Fitness of the original bitmap; the starting best fitness</param>
internal sealed class HolisticHarmonizerRunState
{
    public HolisticHarmonizerRunState(
        HolisticHarmonizerEngineRequest request,
        HarmonyBitmap working,
        HolisticHarmonizerComponents components,
        string agentSummary,
        double fitnessBefore)
    {
        Request = request;
        Working = working;
        Components = components;
        AgentSummary = agentSummary;
        BestFitness = fitnessBefore;
    }

    public HolisticHarmonizerEngineRequest Request { get; }

    public HarmonyBitmap Working { get; }

    public HolisticHarmonizerComponents Components { get; }

    public string AgentSummary { get; }

    public RejectMemory RejectMemory { get; } = new();

    public AdaptiveBatchCap Cap { get; } = new();

    public List<BatchEvaluation> Iterations { get; } = [];

    public HarmonyBitmapPngRenderer PngRenderer { get; } = new();

    public IntentSuccessTracker IntentTracker { get; } = new();

    public RouletteIntentSelector IntentSelector { get; } = new();

    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    public int PlateauCounter { get; set; }

    public int ConsecutiveUnusable { get; set; }

    public bool AbortedOnUnusableResponses { get; set; }

    public double BestFitness { get; set; }

    public int AcceptedCount { get; set; }

    public int RejectedCount { get; set; }

    public string? LastParsingError { get; set; }

    public string? LastRawResponse { get; set; }
}
