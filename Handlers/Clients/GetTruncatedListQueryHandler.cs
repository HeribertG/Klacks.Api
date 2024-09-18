using AutoMapper;
using Klacks.Api.Resources.Filter;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Clients;
using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Handlers.Clients
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
      return await repository.Truncated(request.Filter);
    }
  }
}
