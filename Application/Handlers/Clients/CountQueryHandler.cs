using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class CountQueryHandler : IRequestHandler<CountQuery, int>
    {
        private readonly IClientRepository _clientRepository;

        public CountQueryHandler(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<int> Handle(CountQuery request, CancellationToken cancellationToken)
        {
            return _clientRepository.Count();
        }
    }
}
