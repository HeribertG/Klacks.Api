using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Clients;
using Klacks.Api.Resources.Filter;
using MediatR;

namespace Klacks.Api.Handlers.Clients
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
