// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// How one skill risk class behaves at the current vs. a target autonomy level, on both gates:
/// the chat-turn gate (AutonomyGateService.IsAllowed) and the unattended plan-step gate
/// (PlanStepApprovalPolicy.RequiresApproval) — the two disagree by one level for Irreversible.
/// </summary>
/// <param name="RiskClass">Name of the SkillRiskClass this row covers</param>
/// <param name="SkillCount">Number of currently registered skills classified into this risk class</param>
/// <param name="ChatConfirmationRequiredAtCurrent">Whether a chat call needs confirmation at the current level</param>
/// <param name="ChatConfirmationRequiredAtTarget">Whether a chat call would need confirmation at the target level</param>
/// <param name="PlanApprovalRequiredAtCurrent">Whether an unattended plan step needs approval at the current level</param>
/// <param name="PlanApprovalRequiredAtTarget">Whether an unattended plan step would need approval at the target level</param>
public sealed record AutonomyRiskClassImpact(
    string RiskClass,
    int SkillCount,
    bool ChatConfirmationRequiredAtCurrent,
    bool ChatConfirmationRequiredAtTarget,
    bool PlanApprovalRequiredAtCurrent,
    bool PlanApprovalRequiredAtTarget)
{
    public bool ChatBehaviorChanges => ChatConfirmationRequiredAtCurrent != ChatConfirmationRequiredAtTarget;

    public bool PlanBehaviorChanges => PlanApprovalRequiredAtCurrent != PlanApprovalRequiredAtTarget;
}
