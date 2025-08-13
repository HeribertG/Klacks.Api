using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<GroupResource>, GroupResource?>
{
    private readonly GroupApplicationService _groupApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        GroupApplicationService groupApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _groupApplicationService = groupApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupResource?> Handle(DeleteCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var groupToDelete = await _groupApplicationService.GetGroupByIdAsync(request.Id, cancellationToken);
            if (groupToDelete == null)
            {
                _logger.LogWarning("Group with ID {GroupId} not found for deletion.", request.Id);
                return null;
            }

            await _groupApplicationService.DeleteGroupAsync(request.Id, cancellationToken);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Group with ID {GroupId} deleted successfully.", request.Id);

            return groupToDelete;
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Group with ID {GroupId} not found for deletion.", request.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting group with ID {GroupId}.", request.Id);
            throw;
        }
    }
}
