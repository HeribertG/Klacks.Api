using Klacks_api.Interfaces;
using Klacks_api.Queries.Clients;
using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Handlers.Clients
{
  public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedClient>
  {
    private readonly IClientRepository repository;

    public GetTruncatedListQueryHandler(IClientRepository repository)
    {
      this.repository = repository;
    }

    public async Task<TruncatedClient> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
    {
      return await repository.Truncated(request.Filter);
    }
  }
}
