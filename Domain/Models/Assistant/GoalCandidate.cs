// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A self-generated goal intent proposed by Klacksy's reflection pipeline (Phase 1: shadow mode).
/// A candidate is an intent, not an execution artifact — it carries no plan and no skill names;
/// the PlanningAgent only drafts a plan after a human has approved the candidate (Phase 3).
/// </summary>
/// <param name="UserId">Recipient the candidate would be addressed to, once delivery ships.</param>
/// <param name="Title">Short goal title.</param>
/// <param name="Rationale">Why this goal looks worthwhile right now.</param>
/// <param name="Status">Lifecycle state; see GoalCandidateStatus. Defaults to the shadow value.</param>
/// <param name="Confidence">Model's self-assessed confidence for this candidate; see GoalCandidateConfidence.</param>
/// <param name="SignalSource">What produced the candidate, e.g. the triggering detector's kind name.</param>
/// <param name="DedupHash">Hash over the normalized title and UserId, used to detect repeat candidates.</param>
/// <param name="OwnerPermissionsCsv">Frozen snapshot of the approving user's permissions, taken when the
/// candidate is approved - not when it is created, because only approval identifies who authorized the
/// work. Stays null during shadow mode. Frozen the same way ScheduledTask.OwnerPermissionsCsv is (see
/// ScheduledTaskRunner), because a background reflection service has no ClaimsPrincipal for
/// SkillExecutorService.ValidatePermissions to read.</param>
/// <param name="DecidedUtc">When a human approved or rejected the candidate (Phase 2). Null while
/// the candidate is still shadow/proposed.</param>
/// <param name="PlanId">Id of the AgentPlan drafted from this candidate (Phase 3), once one exists.
/// Null until a plan has been drafted; also guards against drafting a second plan for the same
/// candidate. The plan itself is never executed from here — Phase 3 only shows it to the human.</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class GoalCandidate : BaseEntity
{
    public string? UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Rationale { get; set; } = string.Empty;

    public string Status { get; set; } = GoalCandidateStatus.Shadow;

    public string Confidence { get; set; } = GoalCandidateConfidence.Unknown;

    public string SignalSource { get; set; } = string.Empty;

    public string DedupHash { get; set; } = string.Empty;

    public string? OwnerPermissionsCsv { get; set; }

    public DateTime? DecidedUtc { get; set; }

    public Guid? PlanId { get; set; }
}
