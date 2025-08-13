using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications;

public class PutCommandHandler : IRequestHandler<PutCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly CommunicationApplicationService _communicationApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        CommunicationApplicationService communicationApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _communicationApplicationService = communicationApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CommunicationResource?> Handle(PutCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingCommunication = await _communicationApplicationService.GetCommunicationByIdAsync(request.Resource.Id, cancellationToken);
            if (existingCommunication == null)
            {
                _logger.LogWarning("Communication with ID {CommunicationId} not found.", request.Resource.Id);
                return null;
            }

            var result = await _communicationApplicationService.UpdateCommunicationAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating communication with ID {CommunicationId}.", request.Resource.Id);
            throw;
        }
    }
}
