// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>A client's membership in a group as the planning-rule scope resolution reads it.</summary>
/// <param name="GroupId">Group the client belongs to</param>
/// <param name="ClientId">Member client</param>

namespace Klacks.Api.Domain.Models.Scheduling;

public sealed record PlanningRuleGroupMembership(Guid GroupId, Guid ClientId);
