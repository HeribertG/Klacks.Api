using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class SyncClientsCommandHandler : IRequestHandler<SyncClientsCommand, IdentityProviderSyncResultResource>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly IClientSyncService _clientSyncService;
    private readonly ILogger<SyncClientsCommandHandler> _logger;

    public SyncClientsCommandHandler(
        IIdentityProviderRepository repository,
        IClientSyncService clientSyncService,
        ILogger<SyncClientsCommandHandler> logger)
    {
        _repository = repository;
        _clientSyncService = clientSyncService;
        _logger = logger;
    }

    public async Task<IdentityProviderSyncResultResource> Handle(SyncClientsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting client sync for identity provider: {Id}", request.Id);

        var provider = await _repository.Get(request.Id);
        if (provider == null)
        {
            return new IdentityProviderSyncResultResource
            {
                Success = false,
                ErrorMessage = "Identity provider not found",
                SyncTime = DateTime.UtcNow
            };
        }

        if (!provider.UseForClientImport)
        {
            return new IdentityProviderSyncResultResource
            {
                Success = false,
                ErrorMessage = "This provider is not configured for client import",
                SyncTime = DateTime.UtcNow
            };
        }

        return await _clientSyncService.SyncClientsAsync(provider);
    }
}
