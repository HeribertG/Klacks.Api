// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Services.Clients;

/// <summary>
/// Filters client queries by group membership, including subgroups.
/// </summary>
/// <param name="groupClient">Resolves group hierarchies to flat ID lists</param>
/// <param name="groupVisibility">Determines admin status and visible root groups</param>
/// <param name="logger">Logger for diagnostics</param>
public class ClientGroupFilterService : IClientGroupFilterService
{
    private readonly IGetAllClientIdsFromGroupAndSubgroups _groupClient;
    private readonly IGroupVisibilityService _groupVisibility;
    private readonly ILogger<ClientGroupFilterService> _logger;

    public ClientGroupFilterService(
        IGetAllClientIdsFromGroupAndSubgroups groupClient,
        IGroupVisibilityService groupVisibility,
        ILogger<ClientGroupFilterService> logger)
    {
        _groupClient = groupClient;
        _groupVisibility = groupVisibility;
        _logger = logger;
    }

    public async Task<IQueryable<Client>> FilterClientsByGroupId(Guid? selectedGroupId, IQueryable<Client> query)
    {
        if (selectedGroupId.HasValue)
        {
            var groupIds = await _groupClient.GetAllGroupIdsIncludingSubgroups(selectedGroupId.Value);
            if (groupIds.Count == 0)
            {
                _logger.LogWarning("Selected group {GroupId} returned no group IDs - skipping group filter", selectedGroupId.Value);
                return query;
            }

            query = from client in query
                    where !client.GroupItems.Any() || client.GroupItems.Any(gi => groupIds.Contains(gi.GroupId))
                    select client;
        }
        else
        {
            if (!await _groupVisibility.IsAdmin())
            {
                var rootlist = await _groupVisibility.ReadVisibleRootIdList();
                if (rootlist.Any())
                {
                    var groupIds = await _groupClient.GetAllGroupIdsIncludingSubgroupsFromList(rootlist);
                    query = from client in query
                            where !client.GroupItems.Any() || client.GroupItems.Any(gi => groupIds.Contains(gi.GroupId))
                            select client;
                }
            }
        }

        return query;
    }
}