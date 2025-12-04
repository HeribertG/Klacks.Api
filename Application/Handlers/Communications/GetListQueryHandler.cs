using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CommunicationResource>, IEnumerable<CommunicationResource>>
    {
        private readonly ICommunicationRepository _communicationRepository;
        private readonly IMapper _mapper;

        public GetListQueryHandler(ICommunicationRepository communicationRepository, IMapper mapper)
        {
            _communicationRepository = communicationRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CommunicationResource>> Handle(ListQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            var communications = await _communicationRepository.List();
            return _mapper.Map<IEnumerable<CommunicationResource>>(communications);
        }
    }
}
