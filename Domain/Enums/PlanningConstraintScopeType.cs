// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>Axis a planning constraint is scoped to: every agent, the agents whose active contract references a scheduling rule, the members of a group (including its subgroups) or one client.</summary>
public enum PlanningConstraintScopeType
{
    Global = 1,
    SchedulingRule = 2,
    Group = 3,
    Client = 4,
}
