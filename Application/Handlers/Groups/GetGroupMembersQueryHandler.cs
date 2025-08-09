using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, List<GroupItemResource>>
{
    private readonly IGroupRepository _repository;
    private readonly IMapper _mapper;

    public GetGroupMembersQueryHandler(IGroupRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<GroupItemResource>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
    {
        var group = await _repository.Get(request.GroupId);
        
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID {request.GroupId} not found");
        }

        // Map GroupItems without the circular Group reference
        var groupItemResources = _mapper.Map<List<GroupItemResource>>(group.GroupItems);
        
        return groupItemResources;
    }
}