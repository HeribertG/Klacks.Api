// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one client communication entry (phone, mail, ...) by its id. An entry owned by a client outside
/// the caller's group visibility is answered exactly like an entry that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<CommunicationResource>, CommunicationResource>
    {
        private readonly ICommunicationRepository _communicationRepository;
        private readonly IClientVisibilityGuard _clientVisibilityGuard;
        private readonly AddressCommunicationMapper _addressCommunicationMapper;

        public GetQueryHandler(
            ICommunicationRepository communicationRepository,
            IClientVisibilityGuard clientVisibilityGuard,
            AddressCommunicationMapper addressCommunicationMapper,
            ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _communicationRepository = communicationRepository;
            _clientVisibilityGuard = clientVisibilityGuard;
            _addressCommunicationMapper = addressCommunicationMapper;
        }

        public async Task<CommunicationResource> Handle(GetQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var communication = await _communicationRepository.Get(request.Id);

                if (communication == null
                    || !await _clientVisibilityGuard.IsVisibleAsync(communication.ClientId, cancellationToken))
                {
                    throw new KeyNotFoundException($"Communication with ID {request.Id} not found");
                }

                return _addressCommunicationMapper.ToCommunicationResource(communication);
            }, nameof(Handle), new { request.Id });
        }
    }
}
