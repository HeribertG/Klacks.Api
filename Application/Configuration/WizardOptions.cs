// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration for the Wizard 1 planner engine. Scoring a generation runs in parallel, and the server
/// starts a wizard job per request without an upper bound, so on a busy instance several runs compete
/// for the same cores. This is the knob to cap that without touching the engine.
/// </summary>
/// <param name="EvaluationParallelism">Threads per run for scoring a generation: 0 lets the engine use every core, 1 evaluates sequentially, any other value pins the degree</param>
namespace Klacks.Api.Application.Configuration;

public class WizardOptions
{
    public const string SectionName = "Wizard";

    public int EvaluationParallelism { get; set; }
}
