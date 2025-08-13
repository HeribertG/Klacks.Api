using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class PutCommandHandler : IRequestHandler<PutCommand<GroupResource>, GroupResource?>
{
    private readonly GroupApplicationService _groupApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        GroupApplicationService groupApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _groupApplicationService = groupApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupResource?> Handle(PutCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var updatedGroup = await _groupApplicationService.UpdateGroupAsync(request.Resource, cancellationToken);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Group with ID {GroupId} updated successfully.", request.Resource.Id);

            return updatedGroup;
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Group with ID {GroupId} not found.", request.Resource.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating group with ID {GroupId}.", request.Resource.Id);
            throw;
        }
    }
}
