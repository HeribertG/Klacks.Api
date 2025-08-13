using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedClientResource>
    {
        private readonly ClientApplicationService _clientApplicationService;

        public GetTruncatedListQueryHandler(ClientApplicationService clientApplicationService)
        {
            _clientApplicationService = clientApplicationService;
        }

        public async Task<TruncatedClientResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            return await _clientApplicationService.SearchClientsAsync(request.Filter, cancellationToken);
        }
    }
}
