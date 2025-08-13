using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States;

public class ListQueryHandler : IRequestHandler<ListQuery<StateResource>, IEnumerable<StateResource>>
{
    private readonly StateApplicationService _stateApplicationService;

    public ListQueryHandler(StateApplicationService stateApplicationService)
    {
        _stateApplicationService = stateApplicationService;
    }

    public async Task<IEnumerable<StateResource>> Handle(ListQuery<StateResource> request, CancellationToken cancellationToken)
    {
        return await _stateApplicationService.GetAllStatesAsync(cancellationToken);
    }
}
