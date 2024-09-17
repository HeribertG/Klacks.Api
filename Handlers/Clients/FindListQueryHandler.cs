using Klacks_api.Interfaces;
using Klacks_api.Queries.Clients;
using MediatR;

namespace Klacks_api.Handlers.Clients
{
  public class FindListQueryHandler : IRequestHandler<FindListQuery, IEnumerable<Models.Staffs.Client>>
  {
    private readonly IClientRepository repository;

    public FindListQueryHandler(IClientRepository repository)
    {
      this.repository = repository;
    }

    public async Task<IEnumerable<Models.Staffs.Client>> Handle(FindListQuery request, CancellationToken cancellationToken)
    {
      return await repository.FindList(request.Company, request.Name, request.FirstName);
    }
  }
}
