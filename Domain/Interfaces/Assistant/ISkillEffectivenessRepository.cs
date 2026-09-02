// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only aggregation store behind the "Skill-Wirksamkeit" scorecard (W6). Every method takes the
/// inclusive lower bound of the reporting window, so the scorecard shows a period and not the whole
/// history. Goldset runs are the exception: they are so rare that a window would usually empty the
/// trend table, so the trend is bounded by count instead.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillEffectivenessRepository
{
    /// <summary>Newest goldset runs, newest first.</summary>
    Task<IReadOnlyList<EvalRun>> GetEvalTrendAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>Recipe funnel per recipe for runs created at or after <paramref name="from"/>.</summary>
    Task<IReadOnlyList<RecipeFunnelRow>> GetRecipeFunnelAsync(DateTime from, CancellationToken cancellationToken = default);

    /// <summary>Number of skill usage rows in the window; the denominator of the hallucination rate.</summary>
    Task<int> GetUsageCountAsync(DateTime from, CancellationToken cancellationToken = default);

    /// <summary>Usage rows per failure class in the window; classes without rows are absent.</summary>
    Task<IReadOnlyList<SkillFailureKindCount>> GetFailureCountsAsync(DateTime from, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls and failures per skill in the window. UiAction rows still waiting for the browser report
    /// are excluded, because their Success flag says "not confirmed yet", not "failed".
    /// </summary>
    Task<IReadOnlyList<SkillCallStat>> GetSkillCallStatsAsync(DateTime from, CancellationToken cancellationToken = default);

    /// <summary>Newest trajectory rows in the window, for the provenance distribution.</summary>
    Task<IReadOnlyList<TrajectoryChosenSourceSample>> GetChosenSourceSampleAsync(DateTime from, int limit, CancellationToken cancellationToken = default);
}
