// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides, for a single plan step, whether it must pause for human approval before executing.
/// This is the plan-path counterpart to <c>AutonomyGateService.IsAllowed</c>, but the two tables
/// disagree on purpose: plan steps run with the chat-level autonomy gate bypassed (see
/// <c>PlanStepExecutor.RunLoopAsync</c>, which sets <c>BypassAutonomyGate = true</c>), so this table is
/// the only control left standing for an unattended plan run and is deliberately stricter in one place.
/// Sensitive always pauses here regardless of autonomy level, which matches the chat gate: both hold a
/// sensitive action at every level, so the promise that sensitive actions always need a confirmation
/// holds on both paths.
/// Irreversible requires level FullyAutonomous here, one step stricter than the chat gate's Autonomous,
/// because the plan path has no fallback gate to catch a wrong call.
/// The default arm is fail-closed (pauses) for any risk class not explicitly listed above, so a future
/// enum member is safe by default instead of silently being allowed to run.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant;

public static class PlanStepApprovalPolicy
{
    public static bool RequiresApproval(SkillRiskClass riskClass, AutonomyLevel level) => riskClass switch
    {
        SkillRiskClass.ReadOnly => false,
        SkillRiskClass.Sensitive => true,
        SkillRiskClass.Reversible => level < AutonomyLevel.Assisted,
        SkillRiskClass.ScenarioGated => level < AutonomyLevel.Assisted,
        SkillRiskClass.Irreversible => level < AutonomyLevel.FullyAutonomous,
        _ => true
    };
}
