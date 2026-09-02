// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

public class SkillEffectivenessEvalRun
{
    public string Goldset { get; set; } = string.Empty;

    public string? Model { get; set; }

    public decimal CompositeScore { get; set; }

    public int ItemsTotal { get; set; }

    public int ItemsPassed { get; set; }

    public DateTime? CreateTime { get; set; }
}

public class SkillEffectivenessRecipeFunnelRow
{
    public string RecipeName { get; set; } = string.Empty;

    public int Started { get; set; }

    public int Running { get; set; }

    public int Completed { get; set; }

    public int Aborted { get; set; }

    public int Expired { get; set; }
}

public class SkillEffectivenessFailureSummary
{
    public int TotalRows { get; set; }

    public int NotFound { get; set; }

    public int PermissionDenied { get; set; }

    public int ParameterInvalid { get; set; }

    public int GateHold { get; set; }

    public int UiActionContext { get; set; }

    public int Exception { get; set; }

    public double HallucinationRate { get; set; }
}

public class SkillEffectivenessSkillStat
{
    public string SkillName { get; set; } = string.Empty;

    public int Calls { get; set; }

    public int Successes { get; set; }

    public int Failures { get; set; }

    public double SuccessRate { get; set; }
}

public class SkillEffectivenessSourceRow
{
    public string Source { get; set; } = string.Empty;

    public int Count { get; set; }
}
