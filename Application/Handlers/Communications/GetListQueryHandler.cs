using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<CommunicationResource>, IEnumerable<CommunicationResource>>
    {
        private readonly IMapper mapper;
        private readonly ICommunicationRepository repository;

        public GetListQueryHandler(IMapper mapper,
                                   ICommunicationRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<IEnumerable<CommunicationResource>> Handle(ListQuery<CommunicationResource> request, CancellationToken cancellationToken)
        {
            var communications = await repository.List();

            return mapper.Map<List<Klacks.Api.Domain.Models.Staffs.Communication>, List<CommunicationResource>>(communications!);
        }
    }
}
