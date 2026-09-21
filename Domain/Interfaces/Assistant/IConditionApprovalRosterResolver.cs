// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves who may approve the remediation of one proactive condition, in call order: stage 1 the last
/// planner of the affected shift (only when that is an existing, active, non-system account holding the
/// remediation skill's permissions), stage 2 the planning audience of the condition's group, stage 3 the
/// admins. A finding without a group goes to the admins only. The rights filter runs here, BEFORE anyone
/// is notified, so a person can never acknowledge a chain whose remediation would then be refused for
/// missing permissions.
/// </summary>
/// <param name="condition">The ledger row; TriggerKind, GroupId and EntityId steer the three stages.</param>
/// <param name="requiredPermissions">SkillDescriptor.RequiredPermissions of the remediation skill; an Admin passes regardless.</param>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConditionApprovalRosterResolver
{
    Task<IReadOnlyList<EscalationRosterCandidate>> ResolveAsync(
        AgentCondition condition,
        IReadOnlyCollection<string> requiredPermissions,
        CancellationToken cancellationToken = default);
}
