using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;
public class PostCommandHandler : IRequestHandler<PostCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly GroupVisibilityApplicationService _groupVisibilityApplicationService;

    public PostCommandHandler(GroupVisibilityApplicationService groupVisibilityApplicationService)
    {
        _groupVisibilityApplicationService = groupVisibilityApplicationService;
    }

    public async Task<GroupVisibilityResource?> Handle(PostCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        return await _groupVisibilityApplicationService.CreateGroupVisibilityAsync(request.Resource, cancellationToken);
    }
}