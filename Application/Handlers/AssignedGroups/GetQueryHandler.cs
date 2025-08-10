using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.AssignedGroups;

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
