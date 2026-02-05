using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetPathToNodeQueryHandler : IRequestHandler<GetPathToNodeQuery, List<GroupResource>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;

    public GetPathToNodeQueryHandler(IGroupRepository groupRepository, GroupMapper groupMapper)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
    }

    public async Task<List<GroupResource>> Handle(GetPathToNodeQuery request, CancellationToken cancellationToken)
    {
        var path = await _groupRepository.GetPath(request.NodeId);
        var pathList = path.ToList();
        var result = new List<GroupResource>();
        
        for (int i = 0; i < pathList.Count; i++)
        {
            var groupResource = _groupMapper.ToGroupResource(pathList[i]);
            groupResource.Depth = i;
            result.Add(groupResource);
        }
        
        return result;
    }
}
