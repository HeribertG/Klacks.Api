// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// W6.1: aggregated "Skill-Wirksamkeit" scorecard for the admin settings page. All numbers come from
/// the telemetry built in W1: skill usage records (incl. failure classes), recipe runs, toolset
/// provenance snapshots and the goldset eval runs.
/// </summary>

namespace Klacks.Api.Application.DTOs.Assistant;

public class SkillEffectivenessResource
{
    /// <summary>Length of the reporting window in days the numbers below were aggregated over.</summary>
    public int Days { get; set; }

    /// <summary>Latest goldset runs (eval_runs) as a trend, newest first. Not windowed: runs are
    /// rare enough that a window would usually empty the table.</summary>
    public List<SkillEffectivenessEvalRun> EvalTrend { get; set; } = new();

    /// <summary>Recipe funnel per recipe: started → completed/aborted/expired (W1.5).</summary>
    public List<SkillEffectivenessRecipeFunnelRow> RecipeFunnel { get; set; } = new();

    /// <summary>Failure-class summary across all skill usage records (W1.2).</summary>
    public SkillEffectivenessFailureSummary FailureSummary { get; set; } = new();

    /// <summary>Best skills by success rate (minimum 5 calls).</summary>
    public List<SkillEffectivenessSkillStat> TopSkills { get; set; } = new();

    /// <summary>Worst skills by success rate: minimum call count plus a success rate below the
    /// flop threshold, so the flop table is not simply the reversed top table.</summary>
    public List<SkillEffectivenessSkillStat> FlopSkills { get; set; } = new();

    /// <summary>Where the chosen skill came from, from the W1.6 toolset snapshot.</summary>
    public List<SkillEffectivenessSourceRow> ChosenSourceDistribution { get; set; } = new();
}
