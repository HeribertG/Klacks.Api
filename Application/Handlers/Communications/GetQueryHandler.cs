using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetQueryHandler : IRequestHandler<GetQuery<CommunicationResource>, CommunicationResource>
    {
        private readonly IMapper mapper;
        private readonly ICommunicationRepository repository;

        public GetQueryHandler(IMapper mapper,
                               ICommunicationRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<CommunicationResource> Handle(GetQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            var communication = await repository.Get(request.Id);
            return mapper.Map<Models.Staffs.Communication, CommunicationResource>(communication!);
        }
    }
}
