using AutoMapper;
using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Handlers.Groups;

public class GetPathToNodeQueryHandler(
        IGroupRepository repository,
        DataBaseContext context,
        IMapper mapper) : IRequestHandler<GetPathToNodeQuery, List<GroupTreeNodeResource>>
{
   
    public async Task<List<GroupTreeNodeResource>> Handle(GetPathToNodeQuery request, CancellationToken cancellationToken)
    {
        var pathNodes = await repository.GetPath(request.NodeId);
        var result = new List<GroupTreeNodeResource>();
        var depth = 0;

        foreach (var node in pathNodes.OrderBy(n => n.Lft))
        {
            var clientsCount = await context.GroupItem.CountAsync(gi => gi.GroupId == node.Id, cancellationToken);

            var nodeResource = mapper.Map<GroupTreeNodeResource>(node);

            nodeResource.Depth = depth++;
            nodeResource.ClientsCount = clientsCount;

            result.Add(nodeResource);
        }

        return result;
    }
}