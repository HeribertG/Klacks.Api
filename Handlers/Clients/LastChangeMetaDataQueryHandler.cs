using Klacks_api.Interfaces;
using Klacks_api.Queries.Clients;
using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Handlers.Clients
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
