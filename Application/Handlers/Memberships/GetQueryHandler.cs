using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetQueryHandler : IRequestHandler<GetQuery<MembershipResource>, MembershipResource>
    {
        private readonly IMapper mapper;
        private readonly IMembershipRepository repository;

        public GetQueryHandler(IMapper mapper,
                               IMembershipRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<MembershipResource> Handle(GetQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            var membership = await repository.Get(request.Id);
            return mapper.Map<Models.Associations.Membership, MembershipResource>(membership!);
        }
    }
}
