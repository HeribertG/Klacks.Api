using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients;

/// <summary>
/// CQRS Command Handler for deleting clients
/// Refactored to use Application Service following Clean Architecture
/// </summary>
public class DeleteCommandHandler : IRequestHandler<DeleteCommand<ClientResource>, ClientResource?>
{
    private readonly ClientApplicationService _clientApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        ClientApplicationService clientApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _clientApplicationService = clientApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClientResource?> Handle(DeleteCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        try
        {
            // Get client before deletion for return value
            var clientToDelete = await _clientApplicationService.GetClientByIdAsync(request.Id, cancellationToken);
            if (clientToDelete == null)
            {
                _logger.LogWarning("Client with ID {ClientId} not found for deletion.", request.Id);
                return null;
            }

            // Clean Architecture: Delegate to Application Service
            await _clientApplicationService.DeleteClientAsync(request.Id, cancellationToken);

            // Unit of Work for transaction management
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Client with ID {ClientId} deleted successfully.", request.Id);

            return clientToDelete;
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Client with ID {ClientId} not found for deletion.", request.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting client with ID {ClientId}.", request.Id);
            throw;
        }
    }
}
