// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Accounts;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Services.Clients;

/// <summary>
/// Filters client queries by group membership, including subgroups. A selected group is always
/// intersected with what the caller may see, and a group the caller cannot see yields an empty
/// result rather than an unfiltered query. For a group-restricted (non-admin) user with no specific
/// group selected, clients without any active group are always included, so group-less clients stay
/// visible (consistent with the schedule view). When <paramref name="withoutGroup"/> is set the
/// result is restricted to clients that carry no active (non-scenario) group membership at all, so
/// employees not assigned to any group can be listed. Background services run without a user, so
/// there is nobody whose visibility could apply and the query stays unrestricted for them.
/// </summary>
/// <param name="groupClient">Resolves group hierarchies to flat ID lists</param>
/// <param name="groupVisibility">Determines admin status and visible root groups</param>
/// <param name="user">Identifies the caller; no user means a background job, not a restricted user</param>
/// <param name="logger">Logger for diagnostics</param>
public class ClientGroupFilterService : IClientGroupFilterService
{
    private readonly IGetAllClientIdsFromGroupAndSubgroups _groupClient;
    private readonly IGroupVisibilityService _groupVisibility;
    private readonly IUserService _user;
    private readonly ILogger<ClientGroupFilterService> _logger;

    public ClientGroupFilterService(
        IGetAllClientIdsFromGroupAndSubgroups groupClient,
        IGroupVisibilityService groupVisibility,
        IUserService user,
        ILogger<ClientGroupFilterService> logger)
    {
        _groupClient = groupClient;
        _groupVisibility = groupVisibility;
        _user = user;
        _logger = logger;
    }

    public async Task<IQueryable<Client>> FilterClientsByGroupId(
        Guid? selectedGroupId, IQueryable<Client> query, bool withoutGroup = false)
    {
        if (withoutGroup)
        {
            query = from client in query
                    where !client.GroupItems.Any(gi => gi.AnalyseToken == null)
                    select client;
            return query;
        }

        var scope = await ResolveVisibilityScopeAsync();

        if (selectedGroupId.HasValue)
        {
            var groupIds = await _groupClient.GetAllGroupIdsIncludingSubgroups(selectedGroupId.Value);

            if (!scope.IsUnrestricted)
            {
                var visibleIds = scope.VisibleGroupIds.ToHashSet();
                groupIds = groupIds.Where(visibleIds.Contains).ToHashSet();
            }

            if (groupIds.Count == 0)
            {
                _logger.LogWarning(
                    "Selected group {GroupId} resolved to no group the caller may see - returning an empty result",
                    selectedGroupId.Value);
            }

            return from client in query
                   where client.GroupItems.Any(gi => groupIds.Contains(gi.GroupId) && gi.AnalyseToken == null)
                   select client;
        }

        if (scope.IsUnrestricted)
        {
            return query;
        }

        var visibleGroupIds = scope.VisibleGroupIds.ToList();

        return from client in query
               where !client.GroupItems.Any()
                     || client.GroupItems.Any(gi => visibleGroupIds.Contains(gi.GroupId) && gi.AnalyseToken == null)
               select client;
    }

    private async Task<GroupVisibilityScope> ResolveVisibilityScopeAsync()
    {
        if (string.IsNullOrEmpty(_user.GetIdString()))
        {
            return GroupVisibilityScope.Unrestricted();
        }

        return await _groupVisibility.GetVisibilityScopeAsync();
    }
}