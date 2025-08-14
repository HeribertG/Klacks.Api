using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.AssignedGroups;

public class GetQueryHandler : IRequestHandler<GetQuery<AssignedGroupResource>, AssignedGroupResource>
{
    private readonly IAssignedGroupRepository _assignedGroupRepository;
    private readonly IMapper _mapper;

    public GetQueryHandler(IAssignedGroupRepository assignedGroupRepository, IMapper mapper)
    {
        _assignedGroupRepository = assignedGroupRepository;
        _mapper = mapper;
    }

    public async Task<AssignedGroupResource> Handle(GetQuery<AssignedGroupResource> request, CancellationToken cancellationToken)
    {
        var assignedGroup = await _assignedGroupRepository.Get(request.Id);
        return assignedGroup != null ? _mapper.Map<AssignedGroupResource>(assignedGroup) : new AssignedGroupResource();
    }
}
