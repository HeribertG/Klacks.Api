using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.Models.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class FindListQueryHandler : IRequestHandler<FindListQuery, IEnumerable<Client>>
    {
        private readonly IClientRepository _clientRepository;

        public FindListQueryHandler(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<IEnumerable<Client>> Handle(FindListQuery request, CancellationToken cancellationToken)
        {
            return await _clientRepository.FindList(request.Company, request.Name, request.FirstName);
        }
    }
}
