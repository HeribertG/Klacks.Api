using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States;

public class PutCommandHandler : IRequestHandler<PutCommand<StateResource>, StateResource?>
{
    private readonly StateApplicationService _stateApplicationService;

    public PutCommandHandler(StateApplicationService stateApplicationService)
    {
        _stateApplicationService = stateApplicationService;
    }

    public async Task<StateResource?> Handle(PutCommand<StateResource> request, CancellationToken cancellationToken)
    {
        return await _stateApplicationService.UpdateStateAsync(request.Resource, cancellationToken);
    }
}
