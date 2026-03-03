// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IMembershipRepository membershipRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _membershipRepository = membershipRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<MembershipResource?> Handle(DeleteCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingMembership = await _membershipRepository.Get(request.Id);
            if (existingMembership == null)
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
