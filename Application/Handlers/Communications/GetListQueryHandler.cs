using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CommunicationResource>, IEnumerable<CommunicationResource>>
    {
        private readonly ICommunicationRepository _communicationRepository;
        private readonly AddressCommunicationMapper _addressCommunicationMapper;

        public GetListQueryHandler(ICommunicationRepository communicationRepository, AddressCommunicationMapper addressCommunicationMapper)
        {
            _communicationRepository = communicationRepository;
            _addressCommunicationMapper = addressCommunicationMapper;
        }

        public async Task<IEnumerable<CommunicationResource>> Handle(ListQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            var communications = await _communicationRepository.List();
            return _addressCommunicationMapper.ToCommunicationResources(communications.ToList());
        }
    }
}
