using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.AssignedGroups;

public class GetQueryHandler : IRequestHandler<GetQuery<AssignedGroupResource>, AssignedGroupResource>
{
    private readonly AssignedGroupApplicationService _assignedGroupApplicationService;

    public GetQueryHandler(AssignedGroupApplicationService assignedGroupApplicationService)
    {
        _assignedGroupApplicationService = assignedGroupApplicationService;
    }

    public async Task<AssignedGroupResource> Handle(GetQuery<AssignedGroupResource> request, CancellationToken cancellationToken)
    {
        return await _assignedGroupApplicationService.GetAssignedGroupByIdAsync(request.Id, cancellationToken) ?? new AssignedGroupResource();
    }
}
