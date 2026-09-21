// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// An administrator's advance approval for one trigger kind in one scope: while it is active, the action
/// dispatcher may carry out that kind's remediation WITHOUT asking a human per finding, under the rights
/// of the person who granted it. It replaces the per-finding approval chain, not the brakes around it -
/// the kill switch, the kind's governance MaxAction and daily budget, the circuit breaker, the cascade
/// guard, the attempt limit, the quiet window, the per-tick cap and the unattended skill policy all stay
/// in force, and the execution's identity is minted fresh at execution time, so rights the granter has
/// lost since are gone.
///
/// Fail closed by absence: no row means the installation behaves exactly as it did before standing
/// approvals existed (every executable finding asks its approval chain). There is deliberately no
/// "enabled" flag and no unlimited grant - <see cref="ExpiresAtUtc"/> is mandatory, so a forgotten grant
/// stops by itself.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class StandingApproval : BaseEntity
{
    /// <summary>The canonical trigger kind string from AgentTriggerKinds this grant covers.</summary>
    public string TriggerKind { get; set; } = string.Empty;

    /// <summary>
    /// The scope this grant covers, matched by EXACT equality against AgentCondition.GroupId - never as a
    /// wildcard and never through the group hierarchy. Null is therefore not "all groups" but the same
    /// ungrouped bucket AgentTriggerGovernance's null row and
    /// IAgentConditionRepository.CountActionClaimsAsync(groupId: null) already mean: the findings that
    /// carry no group at all. The governance lookup falls back from a group rule to the installation-wide
    /// one; this must NOT, because that fallback would silently turn one null-group grant into unattended
    /// autonomy for every group in the installation.
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// The administrator whose authority every execution under this grant runs on. Their roles and the
    /// remediation skill's permissions are re-checked at execution time, so this is a claim about who
    /// consented, never a stored permission.
    /// </summary>
    public Guid GrantedByUserId { get; set; }

    public DateTime GrantedAtUtc { get; set; }

    /// <summary>
    /// When the grant stops applying, exclusive: a tick at exactly this instant no longer executes under
    /// it. Mandatory and capped at <see cref="StandingApprovalDefaults.MaximumDurationDays"/> - an
    /// unlimited grant is the one shape nobody would ever notice they still had.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// How many action claims of this kind in this scope may be made per COMPANY day while the grant
    /// applies. Counted from the ledger's own claim events, the same source the governance daily budget
    /// and the circuit breaker are counted from, so it is a second, tighter ceiling on that one number
    /// rather than a counter of its own that could drift from it. The governance budget stays the outer
    /// cap: what actually applies is the smaller of the two.
    /// </summary>
    public int DailyBudget { get; set; } = StandingApprovalDefaults.DefaultDailyBudget;

    /// <summary>
    /// Set when an administrator withdrew the grant. A revoked row is kept rather than deleted, because
    /// it is the audit trail of an autonomy window that was open for a while; it never applies again.
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    public Guid? RevokedByUserId { get; set; }
}
