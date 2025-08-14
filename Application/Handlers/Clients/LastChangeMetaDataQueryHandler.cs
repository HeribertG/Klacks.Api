using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class LastChangeMetaDataQueryHandler : IRequestHandler<LastChangeMetaDataQuery, LastChangeMetaDataResource>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IMapper _mapper;

        public LastChangeMetaDataQueryHandler(IClientRepository clientRepository, IMapper mapper)
        {
            _clientRepository = clientRepository;
            _mapper = mapper;
        }

        public async Task<LastChangeMetaDataResource> Handle(LastChangeMetaDataQuery request, CancellationToken cancellationToken)
        {
            var lastChangeMetaData = await _clientRepository.LastChangeMetaData();
            return _mapper.Map<LastChangeMetaDataResource>(lastChangeMetaData);
        }
    }
}
