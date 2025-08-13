using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.AssignedGroups;

public class PostCommandHandler : IRequestHandler<PostCommand<AssignedGroupResource>, AssignedGroupResource?>
{
    private readonly AssignedGroupApplicationService _assignedGroupApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        AssignedGroupApplicationService assignedGroupApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _assignedGroupApplicationService = assignedGroupApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AssignedGroupResource?> Handle(PostCommand<AssignedGroupResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _assignedGroupApplicationService.CreateAssignedGroupAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new assigned group. ID: {AssignedGroupId}", request.Resource.Id);
            throw;
        }
    }
}

