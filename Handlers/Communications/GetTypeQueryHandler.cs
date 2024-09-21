using Klacks.Api.Interfaces;
using Klacks.Api.Models.Settings;
using Klacks.Api.Queries.Communications;
using MediatR;

namespace Klacks.Api.Handlers.Communications
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
