using AutoMapper;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Clients;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Handlers.Clients
{
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedClientResource>
    {
        private readonly IClientRepository repository;
        private readonly IMapper mapper;

        public GetTruncatedListQueryHandler(IClientRepository repository, IMapper mapper)
        {
            this.repository = repository;
            this.mapper = mapper;
        }

        public async Task<TruncatedClientResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            var result = await repository.Truncated(request.Filter);
            return mapper.Map<TruncatedClientResource>(result);
        }
    }
}
