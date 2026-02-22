// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
        private readonly ScheduleMapper _scheduleMapper;

        public GetQueryHandler(IMembershipRepository membershipRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _membershipRepository = membershipRepository;
            _scheduleMapper = scheduleMapper;
        }

        public async Task<MembershipResource> Handle(GetQuery<MembershipResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var membership = await _membershipRepository.Get(request.Id);

                if (membership == null)
                {
                    throw new KeyNotFoundException($"Membership with ID {request.Id} not found");
                }

                return _scheduleMapper.ToMembershipResource(membership);
            }, nameof(Handle), new { request.Id });
        }
    }
}
