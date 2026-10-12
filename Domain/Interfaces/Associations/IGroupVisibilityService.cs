// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IGroupVisibilityService
{
    Task<List<Guid>> ReadVisibleRootIdList();

    /// <summary>
    /// True as soon as at least one group that is not soft-deleted exists. This is the single trigger
    /// of the fail-closed switch: without groups every caller is unrestricted, with groups a non-admin
    /// without a group_visibility row sees nothing. IGroupVisibilityPreservationService uses the very
    /// same predicate to detect the first group, so the guarantee and the switch can never drift apart.
    /// </summary>
    /// <param name="cancellationToken">Cancels the existence query</param>
    Task<bool> AnyGroupsExistAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Visibility scope of the ambient caller (HTTP claim, else ExecutionPrincipal): unrestricted for an admin and
    /// for an installation without groups; otherwise restricted to the subtrees of the caller's visibility rows,
    /// and empty (fail-closed) when the caller has no row or no id at all.
    /// </summary>
    Task<GroupVisibilityScope> GetVisibilityScopeAsync();

    /// <summary>
    /// The same rule as GetVisibilityScopeAsync, evaluated for an explicitly named user instead of the ambient
    /// caller, so skill executions can resolve the scope of the user they run for. An empty id or an id that
    /// matches no user is answered with an empty restricted scope (fail-closed), even without groups.
    /// </summary>
    /// <param name="userId">Identity id of the user whose scope is resolved</param>
    /// <param name="cancellationToken">Cancels the lookups</param>
    Task<GroupVisibilityScope> GetVisibilityScopeForUserAsync(
        string? userId, CancellationToken cancellationToken = default);
    Task<List<GroupVisibility>> ReviseAdminVisibility(List<GroupVisibility> list);
    Task<List<string>> ReadAdmins();
    Task<bool> IsAdmin();
}
