// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Outcome of the post-apply churn measurement over a captured wizard run. <c>CorrectionChurn</c> is the
/// event-cleaned learner label (planner reworked the proposal); <c>EventChurn</c> is the diagnostic share of
/// changes explained by absences/recovery. Both are ratios over the distinct proposal-cell count.
/// </summary>
public sealed record WizardRunChurnResult(
    double CorrectionChurn,
    double EventChurn,
    int ProposalCellCount,
    int CorrectedCellCount,
    int EventCellCount);
