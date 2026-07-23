// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class FindListQueryHandler : IRequestHandler<FindListQuery, IEnumerable<ClientResource>>
    {
        private readonly IClientSearchRepository _clientSearchRepository;
        private readonly ClientMapper _clientMapper;

        public FindListQueryHandler(IClientSearchRepository clientSearchRepository, ClientMapper clientMapper)
        {
            _clientSearchRepository = clientSearchRepository;
            _clientMapper = clientMapper;
        }

        public async Task<IEnumerable<ClientResource>> Handle(FindListQuery request, CancellationToken cancellationToken)
        {
            var clients = await _clientSearchRepository.FindList(request.Company, request.Name, request.FirstName);
            return clients.Select(_clientMapper.ToResource).ToList();
        }
    }
}
