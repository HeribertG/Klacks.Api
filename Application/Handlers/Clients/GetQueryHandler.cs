// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
        private readonly ClientMapper _clientMapper;

        public GetQueryHandler(IClientRepository clientRepository, ClientMapper clientMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _clientRepository = clientRepository;
            _clientMapper = clientMapper;
        }

        public async Task<ClientResource> Handle(GetQuery<ClientResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var client = await _clientRepository.Get(request.Id);

                if (client == null)
                {
                    throw new KeyNotFoundException($"Client with ID {request.Id} not found");
                }

                return _clientMapper.ToResource(client);
            }, nameof(Handle), new { request.Id });
        }
    }
}
