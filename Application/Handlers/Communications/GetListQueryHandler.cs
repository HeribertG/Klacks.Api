// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists communication entries via the generic ListQuery. Only entries of clients inside the caller's
/// group visibility are returned.
/// </summary>
/// <param name="clientVisibilityGuard">Filters the entries down to those whose client the caller may see</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CommunicationResource>, IEnumerable<CommunicationResource>>
    {
        private readonly ICommunicationRepository _communicationRepository;
        private readonly IClientVisibilityGuard _clientVisibilityGuard;
        private readonly AddressCommunicationMapper _addressCommunicationMapper;

        public GetListQueryHandler(
            ICommunicationRepository communicationRepository,
            IClientVisibilityGuard clientVisibilityGuard,
            AddressCommunicationMapper addressCommunicationMapper)
        {
            _communicationRepository = communicationRepository;
            _clientVisibilityGuard = clientVisibilityGuard;
            _addressCommunicationMapper = addressCommunicationMapper;
        }

        public async Task<IEnumerable<CommunicationResource>> Handle(ListQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            var communications = await _communicationRepository.List();
            var visibleCommunications = await _clientVisibilityGuard.FilterVisibleAsync(
                communications.ToList(), communication => communication.ClientId, cancellationToken);
            return _addressCommunicationMapper.ToCommunicationResources(visibleCommunications);
        }
    }
}
