using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class PutCommandHandler : IRequestHandler<PutCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly GroupVisibilityApplicationService _groupVisibilityApplicationService;

    public PutCommandHandler(GroupVisibilityApplicationService groupVisibilityApplicationService)
    {
        _groupVisibilityApplicationService = groupVisibilityApplicationService;
    }

    public async Task<GroupVisibilityResource?> Handle(PutCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        return await _groupVisibilityApplicationService.UpdateGroupVisibilityAsync(request.Resource, cancellationToken);
    }
}
