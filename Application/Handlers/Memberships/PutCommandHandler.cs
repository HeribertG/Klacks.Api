using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships;

public class PutCommandHandler : IRequestHandler<PutCommand<MembershipResource>, MembershipResource?>
{
    private readonly MembershipApplicationService _membershipApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        MembershipApplicationService membershipApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _membershipApplicationService = membershipApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MembershipResource?> Handle(PutCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingMembership = await _membershipApplicationService.GetMembershipByIdAsync(request.Resource.Id, cancellationToken);
            if (existingMembership == null)
            {
                _logger.LogWarning("Membership with ID {MembershipId} not found.", request.Resource.Id);
                return null;
            }

            var result = await _membershipApplicationService.UpdateMembershipAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating membership with ID {MembershipId}.", request.Resource.Id);
            throw;
        }
    }
}
