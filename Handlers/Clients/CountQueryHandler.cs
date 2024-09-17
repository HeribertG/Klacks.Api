using Klacks_api.Interfaces;
using Klacks_api.Queries.Clients;
using MediatR;

namespace Klacks_api.Handlers.Clients
{
  public class CountQueryHandler : IRequestHandler<CountQuery, int>
  {
    private readonly IClientRepository repository;

    public CountQueryHandler(IClientRepository repository)
    {
      this.repository = repository;
    }

    public Task<int> Handle(CountQuery request, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.Count());
    }
  }
}
