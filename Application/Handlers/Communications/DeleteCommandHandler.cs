using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly CommunicationApplicationService _communicationApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        CommunicationApplicationService communicationApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _communicationApplicationService = communicationApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CommunicationResource?> Handle(DeleteCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingCommunication = await _communicationApplicationService.GetCommunicationByIdAsync(request.Id, cancellationToken);
            if (existingCommunication == null)
            {
                _logger.LogWarning("Communication with ID {CommunicationId} not found for deletion.", request.Id);
                return null;
            }

            await _communicationApplicationService.DeleteCommunicationAsync(request.Id, cancellationToken);
            await _unitOfWork.CompleteAsync();

            return existingCommunication;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting communication with ID {CommunicationId}.", request.Id);
            throw;
        }
    }
}
