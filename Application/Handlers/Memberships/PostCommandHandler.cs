using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Memberships;

public class PostCommandHandler : IRequestHandler<PostCommand<MembershipResource>, MembershipResource?>
{
    private readonly MembershipApplicationService _membershipApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        MembershipApplicationService membershipApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _membershipApplicationService = membershipApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MembershipResource?> Handle(PostCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _membershipApplicationService.CreateMembershipAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new membership.");
            throw;
        }
    }
}
