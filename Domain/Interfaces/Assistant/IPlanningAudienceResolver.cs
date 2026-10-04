// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the set of user ids that belong to a planning role (Admin or Authorised). Used to gate
/// operational proactive alerts so that regular employees never receive them.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPlanningAudienceResolver
{
    Task<IReadOnlySet<string>> GetPlanningUserIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves users in the Admin role only, for alerts that are a data/integration concern
    /// (e.g. an ERP import failure) rather than a scheduling gap every planner should see.
    /// </summary>
    Task<IReadOnlySet<string>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the planning audience for an event scoped to a specific group: every Admin
    /// (unrestricted, as with <see cref="GetPlanningUserIdsAsync"/>) plus every Authorised planner
    /// whose GroupVisibility covers the group, including its whole Nested Set subtree. A planner
    /// with zero GroupVisibility rows is excluded (fail-closed) rather than treated as unrestricted.
    /// </summary>
    Task<IReadOnlySet<string>> GetPlanningUserIdsForGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the planning audience for content about one client, following the client group-visibility
    /// rule: every Admin always; every planner when the client has no group item at all (group-less clients
    /// are visible to everyone); otherwise the union of <see cref="GetPlanningUserIdsForGroupAsync"/> over the
    /// client's non-scenario groups. An unknown or deleted client resolves to the admins only.
    /// </summary>
    Task<IReadOnlySet<string>> GetPlanningUserIdsForClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether one specific user may see at least one of the given groups, independent of whether the user
    /// holds a planning role: an Admin always; anybody else only through a GroupVisibility row of their own
    /// whose root covers one of the groups (Nested Set subtree included). An empty group set, a deleted or
    /// unknown group and a user without any visibility row resolve to false for a non-admin (fail-closed).
    /// For deciding whether a person who once acted on an entity may still be shown it today.
    /// </summary>
    Task<bool> MaySeeAnyGroupAsync(string userId, IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken = default);
}
