using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Communications;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetTypeQueryHandler : IRequestHandler<GetTypeQuery, IEnumerable<CommunicationTypeResource>>
    {
        private readonly ICommunicationRepository _communicationRepository;
        private readonly IMapper _mapper;

        public GetTypeQueryHandler(ICommunicationRepository communicationRepository, IMapper mapper)
        {
            _communicationRepository = communicationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CommunicationTypeResource>> Handle(GetTypeQuery request, CancellationToken cancellationToken)
        {
            var communicationTypes = await _communicationRepository.TypeList();
            return _mapper.Map<IEnumerable<CommunicationTypeResource>>(communicationTypes);
        }
    }
}
