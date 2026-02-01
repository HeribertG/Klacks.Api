using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.Handlers.Clients;

public class GetClientsForReplacementQueryHandler : IRequestHandler<GetClientsForReplacementQuery, IEnumerable<ClientForReplacementResource>>
{
    private readonly IClientSearchRepository _clientSearchRepository;

    public GetClientsForReplacementQueryHandler(IClientSearchRepository clientSearchRepository)
    {
        _clientSearchRepository = clientSearchRepository;
    }

    public async Task<IEnumerable<ClientForReplacementResource>> Handle(GetClientsForReplacementQuery request, CancellationToken cancellationToken)
    {
        return await _clientSearchRepository.GetClientsForReplacement();
    }
}
