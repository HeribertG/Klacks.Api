// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes a membership. A membership owned by a client outside the caller's group visibility is refused exactly like a
/// missing one and nothing is deleted.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Memberships;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<MembershipResource>, MembershipResource?>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IMembershipRepository membershipRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _membershipRepository = membershipRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<MembershipResource?> Handle(DeleteCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingMembership = await _membershipRepository.Get(request.Id);
            if (existingMembership == null || !await _clientVisibilityGuard.IsVisibleAsync(existingMembership.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Membership with ID {request.Id} not found.");
            }

            var membershipResource = _scheduleMapper.ToMembershipResource(existingMembership);
            await _membershipRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return membershipResource;
        }, 
        "deleting membership", 
        new { MembershipId = request.Id });
    }
}
