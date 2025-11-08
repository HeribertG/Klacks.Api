using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Interfaces;

namespace Klacks.Api.Infrastructure.Services;

public class ClientGroupFilterService : IClientGroupFilterService
{
    private readonly IGetAllClientIdsFromGroupAndSubgroups _groupClient;
    private readonly IGroupVisibilityService _groupVisibility;

    public ClientGroupFilterService(
        IGetAllClientIdsFromGroupAndSubgroups groupClient,
        IGroupVisibilityService groupVisibility)
    {
        _groupClient = groupClient;
        _groupVisibility = groupVisibility;
    }

    public async Task<IQueryable<Client>> FilterClientsByGroupId(Guid? selectedGroupId, IQueryable<Client> query)
    {
        if (selectedGroupId.HasValue)
        {
            var groupIds = await _groupClient.GetAllGroupIdsIncludingSubgroups(selectedGroupId.Value);
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