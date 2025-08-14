using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetPathToNodeQueryHandler : IRequestHandler<GetPathToNodeQuery, List<GroupResource>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;

    public GetPathToNodeQueryHandler(IGroupRepository groupRepository, IMapper mapper)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
    }

    public async Task<List<GroupResource>> Handle(GetPathToNodeQuery request, CancellationToken cancellationToken)
    {
        var path = await _groupRepository.GetPath(request.NodeId);
        var pathList = path.ToList();
        var result = new List<GroupResource>();
        
        for (int i = 0; i < pathList.Count; i++)
        {
            var groupResource = _mapper.Map<GroupResource>(pathList[i]);
            groupResource.Depth = i;
            result.Add(groupResource);
        }
        
        return result;
    }
}
