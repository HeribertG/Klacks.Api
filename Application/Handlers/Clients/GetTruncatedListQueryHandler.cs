using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    /// <summary>
    /// CQRS Handler that uses Application Service instead of direct Repository access
    /// Follows Clean Architecture - Handler orchestrates, Application Service contains business logic
    /// </summary>
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedClientResource>
    {
        private readonly ClientApplicationService _clientApplicationService;

        public GetTruncatedListQueryHandler(ClientApplicationService clientApplicationService)
        {
            _clientApplicationService = clientApplicationService;
        }

        public async Task<TruncatedClientResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            // Clean Architecture: Handler delegates to Application Service
            // Application Service handles DTO→Domain→DTO mapping
            return await _clientApplicationService.SearchClientsAsync(request.Filter, cancellationToken);
        }
    }
}
