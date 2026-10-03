// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>Approval state of a planning rule. Only Approved rows are ever evaluated; Proposed rows wait for an admin decision in the UI pending list and expire to Rejected after PlanningConstraintDefaults.ProposalLifetimeDays. Zero is deliberately unused so an unset value never collides with a database default.</summary>
public enum RuleApprovalStatus
{
    Proposed = 1,
    Approved = 2,
    Rejected = 3,
    Revoked = 4,
}
