using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<StateResource>, StateResource?>
{
    private readonly StateApplicationService _stateApplicationService;

    public DeleteCommandHandler(StateApplicationService stateApplicationService)
    {
        _stateApplicationService = stateApplicationService;
    }

    public async Task<StateResource?> Handle(DeleteCommand<StateResource> request, CancellationToken cancellationToken)
    {
        await _stateApplicationService.DeleteStateAsync(request.Id, cancellationToken);
        return null;
    }
}
