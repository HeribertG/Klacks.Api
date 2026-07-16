// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Run-protocol record captured every time a wizard result is applied to the plan. Unlike AnalyseScenario
/// (a what-if container that only exists for the scenario-apply path), this is the single learner source
/// covering both the direct- and scenario-apply paths for all four wizards. The apply-time fields (score
/// snapshot, stage-0 violations, created works) are written on apply; the post-apply measurement fields
/// (correction/event churn, measured-at, outcome) stay null until the deferred measurement job fills them.
/// </summary>
public class WizardRunCapture : BaseEntity
{
    /// <summary>Which wizard engine produced this run.</summary>
    public WizardEngine Engine { get; set; }

    /// <summary>Wizard job identifier the run was produced under.</summary>
    public Guid JobId { get; set; }

    /// <summary>Correlation id grouping runs of the same baseline (W1+2+3). Null for uncorrelated runs.</summary>
    public Guid? RunGroupId { get; set; }

    /// <summary>FK to the AnalyseScenario for scenario-apply; null for direct-apply.</summary>
    public Guid? ScenarioId { get; set; }

    /// <summary>Whether the run was applied directly to the plan or as a scenario.</summary>
    public WizardApplyKind ApplyKind { get; set; }

    /// <summary>Group the run targeted, or null when unknown (e.g. the group-less direct-apply path).</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Start of the applied period.</summary>
    public DateOnly PeriodFrom { get; set; }

    /// <summary>End of the applied period.</summary>
    public DateOnly PeriodUntil { get; set; }

    /// <summary>Engine-tagged JSON snapshot of the sub-score vector captured at apply time. Empty when no snapshot was available.</summary>
    public string SubScoreJson { get; set; } = string.Empty;

    /// <summary>Hard Stage-0 violation count of the applied run.</summary>
    public int Stage0Violations { get; set; }

    /// <summary>Edit-distance of the candidate to the incumbent at apply time. Null on direct first-fill (measures nothing).</summary>
    public double? ChurnAtApply { get; set; }

    /// <summary>Post-apply, event-cleaned correction churn. Null until measured.</summary>
    public double? CorrectionChurn { get; set; }

    /// <summary>Changes explained by absences/recovery (diagnostic). Null until measured.</summary>
    public double? EventChurn { get; set; }

    /// <summary>When the post-apply measurement ran. Null until measured.</summary>
    public DateTime? MeasuredAt { get; set; }

    /// <summary>Terminal outcome of the run. Null until resolved.</summary>
    public CaptureOutcome? Outcome { get; set; }

    /// <summary>Works created by this apply, the measurement job's proposal set.</summary>
    public ICollection<WizardRunCaptureWork> Works { get; set; } = [];
}
