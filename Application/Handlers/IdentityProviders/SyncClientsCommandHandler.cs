using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class SyncClientsCommandHandler : IRequestHandler<SyncClientsCommand, IdentityProviderSyncResultResource>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly ILogger<SyncClientsCommandHandler> _logger;

    public SyncClientsCommandHandler(
        IIdentityProviderRepository repository,
        ILogger<SyncClientsCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IdentityProviderSyncResultResource> Handle(SyncClientsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting client sync for identity provider: {Id}", request.Id);

        return await _repository.SyncClientsAsync(request.Id);
    }
}
