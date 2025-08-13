using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States
{
    public class GetQueryHandler : IRequestHandler<GetQuery<StateResource>, StateResource?>
    {
        private readonly StateApplicationService _stateApplicationService;

        public GetQueryHandler(StateApplicationService stateApplicationService)
        {
            _stateApplicationService = stateApplicationService;
        }

        public async Task<StateResource?> Handle(GetQuery<StateResource> request, CancellationToken cancellationToken)
        {
            return await _stateApplicationService.GetStateByIdAsync(request.Id, cancellationToken);
        }
    }
}
