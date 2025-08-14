using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CommunicationResource>, CommunicationResource>
    {
        private readonly ICommunicationRepository _communicationRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(ICommunicationRepository communicationRepository, IMapper mapper)
        {
            _communicationRepository = communicationRepository;
            _mapper = mapper;
        }

        public async Task<CommunicationResource> Handle(GetQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            var communication = await _communicationRepository.Get(request.Id);
            return communication != null ? _mapper.Map<CommunicationResource>(communication) : new CommunicationResource();
        }
    }
}
