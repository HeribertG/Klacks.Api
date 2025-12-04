using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Memberships;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<MembershipResource>, MembershipResource?>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IMembershipRepository membershipRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _membershipRepository = membershipRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<MembershipResource?> Handle(PutCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingMembership = await _membershipRepository.Get(request.Resource.Id);
            if (existingMembership == null)
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
