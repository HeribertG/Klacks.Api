using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Queries;
using Klacks.Api.Presentation.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.AssignedGroups;

public class GetQueryHandler : IRequestHandler<GetQuery<AssignedGroupResource>, AssignedGroupResource>
{
    private readonly IMapper mapper;
    private readonly IAssignedGroupRepository repository;

    public GetQueryHandler(IMapper mapper,
                           IAssignedGroupRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<AssignedGroupResource> Handle(GetQuery<AssignedGroupResource> request, CancellationToken cancellationToken)
    {
        var assignedGroup = await repository.Get(request.Id);
        return mapper.Map<AssignedGroup, AssignedGroupResource>(assignedGroup!);
    }
}
