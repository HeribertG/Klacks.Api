using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly GroupVisibilityApplicationService _groupVisibilityApplicationService;

    public DeleteCommandHandler(GroupVisibilityApplicationService groupVisibilityApplicationService)
    {
        _groupVisibilityApplicationService = groupVisibilityApplicationService;
    }

    public async Task<GroupVisibilityResource?> Handle(DeleteCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        await _groupVisibilityApplicationService.DeleteGroupVisibilityAsync(request.Id, cancellationToken);
        return null;
    }
}
