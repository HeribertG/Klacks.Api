using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetQueryHandler : IRequestHandler<GetQuery<MembershipResource>, MembershipResource>
    {
        private readonly MembershipApplicationService _membershipApplicationService;

        public GetQueryHandler(MembershipApplicationService membershipApplicationService)
        {
            _membershipApplicationService = membershipApplicationService;
        }

        public async Task<MembershipResource> Handle(GetQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            return await _membershipApplicationService.GetMembershipByIdAsync(request.Id, cancellationToken) ?? new MembershipResource();
        }
    }
}
