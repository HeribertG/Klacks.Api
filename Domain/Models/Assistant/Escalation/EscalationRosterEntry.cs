// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One planner's place in the pageable call list for a group root (docs/ENTWURF-eskalationskette-2026-08-16.md
/// §4). Nested Set has multiple roots, so the list is keyed by the group ROOT, not by every subgroup.
/// DerivedRank is overwritten on every re-derivation from GroupVisibility; OverrideRank is an admin
/// correction that always wins and is never overwritten by that re-derivation. IsOrphaned marks an
/// override whose user no longer has visibility into the group instead of deleting the row, so an
/// admin's manual ordering survives a temporary visibility change. Unlike EscalationChain/Stage, this
/// table IS removed on user deletion: a call list is an organisational fact with no audit character
/// (contrast PeriodAuditLog, which is exactly the opposite case).
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant.Escalation;

public class EscalationRosterEntry : BaseEntity
{
    public Guid GroupRootId { get; set; }

    public required string UserId { get; set; }

    public int? DerivedRank { get; set; }

    public int? OverrideRank { get; set; }

    public bool IsOrphaned { get; set; }

    public int? EffectiveRank => OverrideRank ?? DerivedRank;
}
