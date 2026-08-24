// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of resolving which condition-ledger rows a specific user may see (Etappe 3g context block).
/// Mirrors the audience rule already shipped for proactive notifications (Etappe 3e,
/// PlanningAudienceResolver): only Admin/Authorised planners see findings at all; Admins are
/// unrestricted; an Authorised planner sees only the group root ids covered by their own GroupVisibility
/// rows, fail-closed to an empty set when they have none.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record AgentConditionVisibilityScope(bool IsPlanner, bool IsUnrestricted, IReadOnlySet<Guid> VisibleRootIds)
{
    private static readonly IReadOnlySet<Guid> EmptyRootIds = new HashSet<Guid>();

    /// <summary>Not an Admin or Authorised planner - the context block must not be built for them at all.</summary>
    public static AgentConditionVisibilityScope NotAPlanner() => new(false, false, EmptyRootIds);

    /// <summary>Admin - sees every condition regardless of GroupId.</summary>
    public static AgentConditionVisibilityScope Unrestricted() => new(true, true, EmptyRootIds);

    /// <summary>Authorised planner - sees ungated conditions plus conditions whose group root is in this set.</summary>
    public static AgentConditionVisibilityScope Restricted(IReadOnlySet<Guid> visibleRootIds) => new(true, false, visibleRootIds);
}
