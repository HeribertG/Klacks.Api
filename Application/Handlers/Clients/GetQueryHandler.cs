// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one complete client. A client outside the caller's group visibility is answered exactly like a
/// client that does not exist, so the response never confirms that a hidden client is there.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ClientResource>, ClientResource>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IClientVisibilityGuard _clientVisibilityGuard;
        private readonly ClientMapper _clientMapper;

        public GetQueryHandler(
            IClientRepository clientRepository,
            IClientVisibilityGuard clientVisibilityGuard,
            ClientMapper clientMapper,
            ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _clientRepository = clientRepository;
            _clientVisibilityGuard = clientVisibilityGuard;
            _clientMapper = clientMapper;
        }

        public async Task<ClientResource> Handle(GetQuery<ClientResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var client = await _clientVisibilityGuard.IsVisibleAsync(request.Id, cancellationToken)
                    ? await _clientRepository.Get(request.Id)
                    : null;

                if (client == null)
                {
                    throw new KeyNotFoundException($"Client with ID {request.Id} not found");
                }

                return _clientMapper.ToResource(client);
            }, nameof(Handle), new { request.Id });
        }
    }
}
