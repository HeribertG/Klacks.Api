using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<MembershipResource>, MembershipResource?>
{
    private readonly MembershipApplicationService _membershipApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        MembershipApplicationService membershipApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _membershipApplicationService = membershipApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MembershipResource?> Handle(DeleteCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingMembership = await _membershipApplicationService.GetMembershipByIdAsync(request.Id, cancellationToken);
            if (existingMembership == null)
            {
                _logger.LogWarning("Membership with ID {MembershipId} not found for deletion.", request.Id);
                return null;
            }

            await _membershipApplicationService.DeleteMembershipAsync(request.Id, cancellationToken);
            await _unitOfWork.CompleteAsync();

            return existingMembership;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting membership with ID {MembershipId}.", request.Id);
            throw;
        }
    }
}
