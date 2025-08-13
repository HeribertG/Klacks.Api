using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<MembershipResource>, IEnumerable<MembershipResource>>
    {
        private readonly MembershipApplicationService _membershipApplicationService;

        public GetListQueryHandler(MembershipApplicationService membershipApplicationService)
        {
            _membershipApplicationService = membershipApplicationService;
        }

        public async Task<IEnumerable<MembershipResource>> Handle(ListQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            return await _membershipApplicationService.GetAllMembershipsAsync(cancellationToken);
        }
    }
}
