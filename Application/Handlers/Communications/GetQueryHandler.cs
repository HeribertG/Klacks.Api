// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
        private readonly AddressCommunicationMapper _addressCommunicationMapper;

        public GetQueryHandler(ICommunicationRepository communicationRepository, AddressCommunicationMapper addressCommunicationMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _communicationRepository = communicationRepository;
            _addressCommunicationMapper = addressCommunicationMapper;
        }

        public async Task<CommunicationResource> Handle(GetQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var communication = await _communicationRepository.Get(request.Id);

                if (communication == null)
                {
                    throw new KeyNotFoundException($"Communication with ID {request.Id} not found");
                }

                return _addressCommunicationMapper.ToCommunicationResource(communication);
            }, nameof(Handle), new { request.Id });
        }
    }
}
