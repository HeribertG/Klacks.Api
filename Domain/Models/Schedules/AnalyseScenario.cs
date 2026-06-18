// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Represents a what-if analysis scenario for schedule planning.
/// </summary>
/// <param name="Token">Unique token used to tag all cloned schedule data belonging to this scenario</param>
/// <param name="GroupId">The group this scenario belongs to, or null for a group-unfiltered scenario</param>
/// <param name="FromDate">Start of the scenario time range</param>
/// <param name="UntilDate">End of the scenario time range</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Models.Schedules;

public class AnalyseScenario : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? GroupId { get; set; }

    public Group? Group { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }

    public Guid Token { get; set; }

    /// <summary>
    /// Correlation id that groups all scenarios produced from the same wizard test run
    /// (Wizard 1 + 2 + 3 sharing a baseline). Auto-generated for scenarios that originate
    /// from the main schedule and inherited from the source scenario otherwise. Null on
    /// pre-existing scenarios written before this column was introduced.
    /// </summary>
    public Guid? RunGroupId { get; set; }

    public string CreatedByUser { get; set; } = string.Empty;

    public AnalyseScenarioStatus Status { get; set; } = AnalyseScenarioStatus.Active;

    /// <summary>
    /// Engine-tagged JSON snapshot of the sub-score vector captured at apply time (when the scenario
    /// was created), e.g. composite gate/scalar/sub-scores for W4 or stage scores for W1/W2/W3. Feeds
    /// the future preference-learner. Null until the apply-time capture writes it.
    /// </summary>
    public string? SubScoreJson { get; set; }

    /// <summary>Edit-distance ratio of this candidate to the incumbent plan at apply time (content key incl. ShiftId). Null until captured.</summary>
    public double? ChurnRatio { get; set; }

    /// <summary>Hard Stage-0 violation count at apply time. Null until captured.</summary>
    public int? Stage0Violations { get; set; }

    /// <summary>Structured reason the operator rejected this scenario. Null unless Status == Rejected with a reason.</summary>
    public RejectReason? RejectReason { get; set; }

    /// <summary>Optional free-text rejection note.</summary>
    public string? RejectReasonText { get; set; }
}
