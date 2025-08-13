using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients;

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
            var clientToDelete = await _clientApplicationService.GetClientByIdAsync(request.Id, cancellationToken);
            if (clientToDelete == null)
            {
                _logger.LogWarning("Client with ID {ClientId} not found for deletion.", request.Id);
                return null;
            }

            await _clientApplicationService.DeleteClientAsync(request.Id, cancellationToken);
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
