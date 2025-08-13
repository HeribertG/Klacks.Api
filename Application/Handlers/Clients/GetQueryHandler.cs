using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class GetQueryHandler : IRequestHandler<GetQuery<ClientResource>, ClientResource>
    {
        private readonly ClientApplicationService _clientApplicationService;

        public GetQueryHandler(ClientApplicationService clientApplicationService)
        {
            _clientApplicationService = clientApplicationService;
        }

        public async Task<ClientResource> Handle(GetQuery<ClientResource> request, CancellationToken cancellationToken)
        {
            var client = await _clientApplicationService.GetClientByIdAsync(request.Id, cancellationToken);
            
            if (client == null)
            {
                throw new KeyNotFoundException($"Client with ID {request.Id} not found.");
            }
            
            return client;
        }
    }
}
