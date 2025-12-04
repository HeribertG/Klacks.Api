using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class FindListQueryHandler : IRequestHandler<FindListQuery, IEnumerable<Client>>
    {
        private readonly IClientSearchRepository _clientSearchRepository;

        public FindListQueryHandler(IClientSearchRepository clientSearchRepository)
        {
            _clientSearchRepository = clientSearchRepository;
        }

        public async Task<IEnumerable<Client>> Handle(FindListQuery request, CancellationToken cancellationToken)
        {
            return await _clientSearchRepository.FindList(request.Company, request.Name, request.FirstName);
        }
    }
}
