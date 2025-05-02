using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Handlers.Groups;

public class GetPathToNodeQueryHandler : IRequestHandler<GetPathToNodeQuery, List<GroupTreeNodeResource>>
{
    private readonly IGroupRepository _repository;
    private readonly DataBaseContext _context;

    public GetPathToNodeQueryHandler(IGroupRepository repository, DataBaseContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<List<GroupTreeNodeResource>> Handle(GetPathToNodeQuery request, CancellationToken cancellationToken)
    {
        var pathNodes = await _repository.GetPath(request.NodeId);

        var result = new List<GroupTreeNodeResource>();
        var depth = 0;

        foreach (var node in pathNodes.OrderBy(n => n.Lft))
        {
            var clientsCount = await _context.GroupItem.CountAsync(gi => gi.GroupId == node.Id, cancellationToken);

            result.Add(new GroupTreeNodeResource
            {
                Id = node.Id,
                Name = node.Name,
                Description = node.Description,
                ValidFrom = node.ValidFrom,
                ValidUntil = node.ValidUntil,
                ParentId = node.Parent,
                Root = node.Root,
                Lft = node.Lft,
                Rgt = node.rgt,
                Depth = depth++,
                ClientsCount = clientsCount,
                CreateTime = node.CreateTime,
                UpdateTime = node.UpdateTime
            });
        }

        return result;
    }
}