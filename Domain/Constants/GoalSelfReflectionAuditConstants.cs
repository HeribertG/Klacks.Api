// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared audit identity for a self-reflection-originated plan's unattended execution. Used both when
/// GoalPlanExecutionService starts the original run for an approved GoalCandidate and when
/// PlanStepExecutor.ApproveAndContinueAsync resumes that same plan after a human clears a
/// Sensitive-step pause - the remaining steps must keep running under the identical audit name and
/// SessionId prefix, otherwise the audit trail would attribute them to whoever resumed the plan instead
/// of the automation.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GoalSelfReflectionAuditConstants
{
    public const string AuditUserName = "Klacksy self-reflection";
    public const string SessionIdPrefix = "self-reflection:";

    /// <summary>
    /// Parses a GoalCandidate's frozen OwnerPermissionsCsv the same way everywhere it is consumed, so
    /// the permission list handed to SkillExecutionContext never drifts between the original run and a
    /// resumed one.
    /// </summary>
    /// <param name="ownerPermissionsCsv">Comma-separated permissions frozen at goal-candidate approval time.</param>
    public static IReadOnlyList<string> ParseOwnerPermissions(string ownerPermissionsCsv) =>
        ownerPermissionsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
