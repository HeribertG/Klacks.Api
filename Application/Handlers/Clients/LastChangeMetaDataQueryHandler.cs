using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class LastChangeMetaDataQueryHandler : IRequestHandler<LastChangeMetaDataQuery, LastChangeMetaDataResource>
    {
        private readonly ClientApplicationService _clientApplicationService;

        public LastChangeMetaDataQueryHandler(ClientApplicationService clientApplicationService)
        {
            _clientApplicationService = clientApplicationService;
        }

        public async Task<LastChangeMetaDataResource> Handle(LastChangeMetaDataQuery request, CancellationToken cancellationToken)
        {
            return await _clientApplicationService.GetLastChangeMetaDataAsync(cancellationToken);
        }
    }
}
