using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<MembershipResource>, IEnumerable<MembershipResource>>
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly IMapper _mapper;

        public GetListQueryHandler(IMembershipRepository membershipRepository, IMapper mapper)
        {
            _membershipRepository = membershipRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MembershipResource>> Handle(ListQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            var memberships = await _membershipRepository.List();
            return _mapper.Map<IEnumerable<MembershipResource>>(memberships);
        }
    }
}
