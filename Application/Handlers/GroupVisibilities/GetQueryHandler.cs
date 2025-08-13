using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class GetQueryHandler : IRequestHandler<GetQuery<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly GroupVisibilityApplicationService _groupVisibilityApplicationService;

    public GetQueryHandler(GroupVisibilityApplicationService groupVisibilityApplicationService)
    {
        _groupVisibilityApplicationService = groupVisibilityApplicationService;
    }

    public async Task<GroupVisibilityResource?> Handle(GetQuery<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        return await _groupVisibilityApplicationService.GetGroupVisibilityByIdAsync(request.Id, cancellationToken);
    }
}
