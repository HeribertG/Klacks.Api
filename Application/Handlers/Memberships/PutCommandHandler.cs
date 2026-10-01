// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates a membership. Both the stored owning client and the client named in the request must be inside the caller's
/// group visibility; otherwise the membership is refused exactly like a missing one and nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Memberships;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<MembershipResource>, MembershipResource?>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IMembershipRepository membershipRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _membershipRepository = membershipRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<MembershipResource?> Handle(PutCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingMembership = await _membershipRepository.Get(request.Resource.Id);
            if (existingMembership == null
                || !await _clientVisibilityGuard.AreAllVisibleAsync(
                    [existingMembership.ClientId, request.Resource.ClientId], cancellationToken))
            {
                throw new KeyNotFoundException($"Membership with ID {request.Resource.Id} not found.");
            }

            var updatedMembership = _scheduleMapper.ToMembershipEntity(request.Resource);
            updatedMembership.CreateTime = existingMembership.CreateTime;
            updatedMembership.CurrentUserCreated = existingMembership.CurrentUserCreated;
            existingMembership = updatedMembership;
            await _membershipRepository.Put(existingMembership);
            await _unitOfWork.CompleteAsync();
            return _scheduleMapper.ToMembershipResource(existingMembership);
        }, 
        "updating membership", 
        new { MembershipId = request.Resource.Id });
    }
}
