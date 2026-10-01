// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Duplicate check of "new client". Deliberately not limited by group visibility (owner decision 2026-10-01: a
/// duplicate hidden in a foreign group must still be found), and therefore answered with identity fields only
/// instead of the full client resource. The internal id is withheld for a hit the caller cannot see (owner
/// decision 2026-10-01): it is only needed to open the client, which the caller may not do.
/// </summary>
/// <param name="request">Company, name and first name fragments; all blank yields no result</param>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class FindListQueryHandler : IRequestHandler<FindListQuery, IEnumerable<ClientDuplicateCandidateResource>>
    {
        private readonly IClientSearchRepository _clientSearchRepository;
        private readonly IClientVisibilityGuard _clientVisibilityGuard;

        public FindListQueryHandler(
            IClientSearchRepository clientSearchRepository, IClientVisibilityGuard clientVisibilityGuard)
        {
            _clientSearchRepository = clientSearchRepository;
            _clientVisibilityGuard = clientVisibilityGuard;
        }

        public async Task<IEnumerable<ClientDuplicateCandidateResource>> Handle(FindListQuery request, CancellationToken cancellationToken)
        {
            var clients = await _clientSearchRepository.FindList(request.Company, request.Name, request.FirstName);
            var visibleIds = (await _clientVisibilityGuard.FilterVisibleAsync(clients, client => client.Id, cancellationToken))
                .Select(client => client.Id)
                .ToHashSet();

            return clients
                .Select(client => new ClientDuplicateCandidateResource(
                    visibleIds.Contains(client.Id) ? client.Id : null,
                    client.IdNumber,
                    client.Company,
                    client.Name,
                    client.FirstName,
                    (int)client.Type))
                .ToList();
        }
    }
}
