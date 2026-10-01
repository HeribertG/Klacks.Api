// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates a membership. A membership for a client outside the caller's group visibility is refused exactly like one for a
/// client that does not exist, and nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Memberships;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<MembershipResource>, MembershipResource?>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        IMembershipRepository membershipRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _membershipRepository = membershipRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<MembershipResource?> Handle(PostCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _clientVisibilityGuard.IsVisibleAsync(request.Resource.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Client with ID {request.Resource.ClientId} not found");
            }

            var membership = _scheduleMapper.ToMembershipEntity(request.Resource);
            await _membershipRepository.Add(membership);
            await _unitOfWork.CompleteAsync();
            return _scheduleMapper.ToMembershipResource(membership);
        }, 
        "creating membership", 
        new { });
    }
}
