using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, List<GroupItemResource>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;

    public GetGroupMembersQueryHandler(IGroupRepository groupRepository, IMapper mapper)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
    }

    public async Task<List<GroupItemResource>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.Get(request.GroupId);
        
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID {request.GroupId} not found");
        }
        
        return _mapper.Map<List<GroupItemResource>>(group.GroupItems);
    }
}