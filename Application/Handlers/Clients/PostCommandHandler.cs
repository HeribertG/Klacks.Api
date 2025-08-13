using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients;

/// <summary>
/// CQRS Command Handler for creating new clients
/// Refactored to use Application Service following Clean Architecture
/// </summary>
public class PostCommandHandler : IRequestHandler<PostCommand<ClientResource>, ClientResource?>
{
    private readonly ClientApplicationService _clientApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        ClientApplicationService clientApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _clientApplicationService = clientApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClientResource?> Handle(PostCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        try
        {
            // Clean Architecture: Delegate business logic to Application Service
            var createdClient = await _clientApplicationService.CreateClientAsync(request.Resource, cancellationToken);

            // Unit of Work for transaction management
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("New client created successfully. ID: {ClientId}", createdClient.Id);

            return createdClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating a new client.");
            throw;
        }
    }
}
