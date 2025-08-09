using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class LastChangeMetaDataQueryHandler : IRequestHandler<LastChangeMetaDataQuery, LastChangeMetaDataResource>
    {
        private readonly IClientRepository repository;

        public LastChangeMetaDataQueryHandler(IClientRepository repository)
        {
            this.repository = repository;
        }

        public async Task<LastChangeMetaDataResource> Handle(LastChangeMetaDataQuery request, CancellationToken cancellationToken)
        {
            return await repository.LastChangeMetaData();
        }
    }
}
