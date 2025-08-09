using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedGroupResource>
    {
        private readonly IMapper mapper;
        private readonly IGroupRepository repository;

        public GetTruncatedListQueryHandler(
                                            IMapper mapper,
                                            IGroupRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<TruncatedGroupResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            var truncated = await repository.Truncated(request.Filter);
            return mapper.Map<TruncatedGroup, TruncatedGroupResource>(truncated!);
        }
    }
}
