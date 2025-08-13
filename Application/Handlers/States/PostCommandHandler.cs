using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States;

public class PostCommandHandler : IRequestHandler<PostCommand<StateResource>, StateResource?>
{
    private readonly StateApplicationService _stateApplicationService;

    public PostCommandHandler(StateApplicationService stateApplicationService)
    {
        _stateApplicationService = stateApplicationService;
    }

    public async Task<StateResource?> Handle(PostCommand<StateResource> request, CancellationToken cancellationToken)
    {
        return await _stateApplicationService.CreateStateAsync(request.Resource, cancellationToken);
    }
}
