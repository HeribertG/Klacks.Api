using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetQueryHandler : IRequestHandler<GetQuery<MembershipResource>, MembershipResource>
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(IMembershipRepository membershipRepository, IMapper mapper)
        {
            _membershipRepository = membershipRepository;
            _mapper = mapper;
        }

        public async Task<MembershipResource> Handle(GetQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            var membership = await _membershipRepository.Get(request.Id);
            return membership != null ? _mapper.Map<MembershipResource>(membership) : new MembershipResource();
        }
    }
}
