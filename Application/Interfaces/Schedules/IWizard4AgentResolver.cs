// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Resolves the schedulable employee (Client) ids of a group for a period — the agent set the
/// Wizard-4 background optimiser plans over, matching the employees the operator sees in that group's
/// schedule. Backed by the group membership (GroupItem.ClientId) restricted to memberships valid in
/// the window.
/// </summary>
public interface IWizard4AgentResolver
{
    Task<IReadOnlyList<Guid>> ResolveAsync(Guid groupId, DateOnly periodFrom, DateOnly periodUntil, CancellationToken ct);
}
