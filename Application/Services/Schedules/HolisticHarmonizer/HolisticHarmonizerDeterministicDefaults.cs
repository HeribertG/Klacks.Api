// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.HolisticHarmonizer.Search;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Production limits of the deterministic stage-3 search. The wall-clock budget stays below the job runner's
/// hard budget (180 s) so context loading and the post-run eligibility scan still fit.
/// </summary>
public static class HolisticHarmonizerDeterministicDefaults
{
    /// <summary>Label reported in place of an LLM model id so run results and logs show which mode produced them.</summary>
    public const string EngineLabel = "deterministic-local-search";

    public static DeterministicSearchOptions Options { get; } = DeterministicSearchOptions.Default;
}
