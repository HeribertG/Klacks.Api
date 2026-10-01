// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one membership. A membership owned by a client outside the caller's group visibility is answered
/// exactly like a membership that does not exist.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Memberships
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<MembershipResource>, MembershipResource>
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly IClientVisibilityGuard _clientVisibilityGuard;
        private readonly ScheduleMapper _scheduleMapper;

        public GetQueryHandler(
            IMembershipRepository membershipRepository,
            IClientVisibilityGuard clientVisibilityGuard,
            ScheduleMapper scheduleMapper,
            ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _membershipRepository = membershipRepository;
            _clientVisibilityGuard = clientVisibilityGuard;
            _scheduleMapper = scheduleMapper;
        }

        public async Task<MembershipResource> Handle(GetQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var membership = await _membershipRepository.Get(request.Id);

                if (membership == null || !await _clientVisibilityGuard.IsVisibleAsync(membership.ClientId, cancellationToken))
                {
                    throw new KeyNotFoundException($"Membership with ID {request.Id} not found");
                }

                return _scheduleMapper.ToMembershipResource(membership);
            }, nameof(Handle), new { request.Id });
        }
    }
}
