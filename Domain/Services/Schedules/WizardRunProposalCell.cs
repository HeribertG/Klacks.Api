// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// One proposal cell from a captured wizard run, projected from the Work the apply created. Carries the
/// churn cell-key (agent, date, shift, start, end) plus the correction signals. A cell counts as corrected
/// when it is either soft-deleted (<paramref name="IsDeleted"/>) or overlaid by a non-recovery WorkChange
/// (<paramref name="IsOverlaid"/>): the standard planner correction path replaces the incumbent with a
/// WorkChange overlay row (ReplacementStart/End/Within) while the underlying proposal work stays undeleted,
/// so IsDeleted alone would miss it. An UpdateTime-based in-place edit signal is deliberately not used,
/// because the apply pipeline itself (overtime cascade / container expansion) re-saves the fresh work and
/// stamps UpdateTime at apply time, which would make it fire for every direct-apply cell — a documented
/// measurement limit. Recovery-marked overlays are excluded from IsOverlaid; they surface as event churn.
/// </summary>
public sealed record WizardRunProposalCell(
    Guid AgentId,
    DateOnly Date,
    Guid ShiftId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsDeleted,
    bool IsOverlaid = false);
