using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<MembershipResource>, IEnumerable<MembershipResource>>
    {
        private readonly IMapper mapper;
        private readonly IMembershipRepository repository;

        public GetListQueryHandler(IMapper mapper,
                                   IMembershipRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<IEnumerable<MembershipResource>> Handle(ListQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            var memberships = await repository.List();

            return mapper.Map<List<Models.Associations.Membership>, List<MembershipResource>>(memberships!);
        }
    }
}
