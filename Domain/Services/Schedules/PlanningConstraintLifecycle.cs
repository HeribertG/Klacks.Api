// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// State transitions of a PlanningConstraint: Proposed -> Approved | Rejected, Approved -> Revoked. A
/// proposal older than PlanningConstraintDefaults.ProposalLifetimeDays counts as expired even before the
/// expiry sweep has run, so it can no longer be approved. Every method returns false instead of changing the
/// row when the transition is not allowed.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Services.Schedules;

public static class PlanningConstraintLifecycle
{
    public static readonly TimeSpan ProposalLifetime = TimeSpan.FromDays(PlanningConstraintDefaults.ProposalLifetimeDays);

    public static bool IsProposalExpired(PlanningConstraint constraint, DateTime nowUtc) =>
        constraint.ApprovalStatus == RuleApprovalStatus.Proposed
        && constraint.CreateTime.HasValue
        && constraint.CreateTime.Value.Add(ProposalLifetime) <= nowUtc;

    public static bool TryApprove(PlanningConstraint constraint, string approvedBy, DateTime nowUtc)
    {
        if (constraint.ApprovalStatus != RuleApprovalStatus.Proposed || IsProposalExpired(constraint, nowUtc))
        {
            return false;
        }

        constraint.ApprovalStatus = RuleApprovalStatus.Approved;
        constraint.ApprovedBy = approvedBy;
        constraint.ApprovedAt = nowUtc;
        return true;
    }

    public static bool TryReject(PlanningConstraint constraint)
    {
        if (constraint.ApprovalStatus != RuleApprovalStatus.Proposed)
        {
            return false;
        }

        constraint.ApprovalStatus = RuleApprovalStatus.Rejected;
        return true;
    }

    public static bool TryRevoke(PlanningConstraint constraint)
    {
        if (constraint.ApprovalStatus != RuleApprovalStatus.Approved)
        {
            return false;
        }

        constraint.ApprovalStatus = RuleApprovalStatus.Revoked;
        return true;
    }

    public static void MarkAdminApproved(PlanningConstraint constraint, string approvedBy, DateTime nowUtc)
    {
        constraint.Origin = RuleOrigin.Admin;
        constraint.ApprovalStatus = RuleApprovalStatus.Approved;
        constraint.ProposedBy = approvedBy;
        constraint.ApprovedBy = approvedBy;
        constraint.ApprovedAt = nowUtc;
    }
}
