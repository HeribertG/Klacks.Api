// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

    Task<GroupVisibilityScope> GetVisibilityScopeAsync();
    Task<List<GroupVisibility>> ReviseAdminVisibility(List<GroupVisibility> list);
    Task<List<string>> ReadAdmins();
    Task<bool> IsAdmin();
}
