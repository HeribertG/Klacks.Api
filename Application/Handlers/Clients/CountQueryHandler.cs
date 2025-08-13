using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class CountQueryHandler : IRequestHandler<CountQuery, int>
    {
        private readonly ClientApplicationService _clientApplicationService;

        public CountQueryHandler(ClientApplicationService clientApplicationService)
        {
            _clientApplicationService = clientApplicationService;
        }

        public async Task<int> Handle(CountQuery request, CancellationToken cancellationToken)
        {
            return await _clientApplicationService.GetClientCountAsync(cancellationToken);
        }
    }
}
