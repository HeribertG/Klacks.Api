using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients;

public class PutCommandHandler : IRequestHandler<PutCommand<ClientResource>, ClientResource?>
{
    private readonly ClientApplicationService _clientApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        ClientApplicationService clientApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _clientApplicationService = clientApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClientResource?> Handle(PutCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var updatedClient = await _clientApplicationService.UpdateClientAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Client with ID {ClientId} updated successfully.", request.Resource.Id);
            return updatedClient;
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Client with ID {ClientId} not found.", request.Resource.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating client with ID {ClientId}.", request.Resource.Id);
            throw;
        }
    }
}
