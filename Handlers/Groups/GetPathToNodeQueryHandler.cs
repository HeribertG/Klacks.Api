using AutoMapper;
using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Queries.Groups;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Handlers.Groups;

public class GetPathToNodeQueryHandler(
        IGroupRepository repository,
        DataBaseContext context,
        IMapper mapper) : IRequestHandler<GetPathToNodeQuery, List<GroupResource>>
{

    public async Task<List<GroupResource>> Handle(GetPathToNodeQuery request, CancellationToken cancellationToken)
    {
        var pathNodes = await repository.GetPath(request.NodeId);
        var result = new List<GroupResource>();
        var depth = 0;

        foreach (var node in pathNodes.OrderBy(n => n.Lft))
        {
            var clientsCount = await context.GroupItem.CountAsync(gi => gi.GroupId == node.Id, cancellationToken);

            var nodeResource = mapper.Map<GroupResource>(node);

            nodeResource.Depth = depth++;

            result.Add(nodeResource);
        }

        return result;
    }
}