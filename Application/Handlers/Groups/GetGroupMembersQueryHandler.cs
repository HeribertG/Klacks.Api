using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, List<GroupItemResource>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;

    public GetGroupMembersQueryHandler(IGroupRepository groupRepository, GroupMapper groupMapper)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
    }

    public async Task<List<GroupItemResource>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.Get(request.GroupId);

        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID {request.GroupId} not found");
        }

        return group.GroupItems.Select(g => _groupMapper.ToGroupItemResource(g)).ToList();
    }
}