// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Infrastructure.Mediator;

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
            return await _clientRepository.CountAsync();
        }
    }
}
