using Klacks_api.Interfaces;
using Klacks_api.Models.Settings;
using Klacks_api.Queries.Communications;
using MediatR;

namespace Klacks_api.Handlers.Communications
{
  public class GetTypeQueryHandler : IRequestHandler<GetTypeQuery, IEnumerable<CommunicationType>>
  {
    private readonly ICommunicationRepository repository;

    public GetTypeQueryHandler(ICommunicationRepository repository)
    {
      this.repository = repository;
    }

    public async Task<IEnumerable<CommunicationType>> Handle(GetTypeQuery request, CancellationToken cancellationToken)
    {
      return await repository.TypeList();
    }
  }
}
