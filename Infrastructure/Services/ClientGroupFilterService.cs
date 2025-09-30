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
            var clientIds = await _groupClient.GetAllClientIdsFromGroupAndSubgroups(selectedGroupId.Value);
            query = query.Where(client => clientIds.Contains(client.Id));
        }
        else
        {
            if (!await _groupVisibility.IsAdmin())
            {
                var rootlist = await _groupVisibility.ReadVisibleRootIdList();
                if (rootlist.Any())
                {
                    var clientIds = await _groupClient.GetAllClientIdsFromGroupsAndSubgroupsFromList(rootlist);
                    query = query.Where(client => clientIds.Contains(client.Id));
                }
            }
        }

        return query;
    }
}