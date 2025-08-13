using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications;

public class PostCommandHandler : IRequestHandler<PostCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly CommunicationApplicationService _communicationApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        CommunicationApplicationService communicationApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _communicationApplicationService = communicationApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CommunicationResource?> Handle(PostCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _communicationApplicationService.CreateCommunicationAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new communication.");
            throw;
        }
    }
}
