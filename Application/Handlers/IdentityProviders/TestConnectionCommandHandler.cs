using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class TestConnectionCommandHandler : IRequestHandler<TestConnectionCommand, TestConnectionResultResource>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly ILdapService _ldapService;
    private readonly ILogger<TestConnectionCommandHandler> _logger;

    public TestConnectionCommandHandler(
        IIdentityProviderRepository repository,
        ILdapService ldapService,
        ILogger<TestConnectionCommandHandler> logger)
    {
        _repository = repository;
        _ldapService = ldapService;
        _logger = logger;
    }

    public async Task<TestConnectionResultResource> Handle(TestConnectionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing connection for identity provider: {Id}", request.Id);

        var provider = await _repository.Get(request.Id);
        if (provider == null)
        {
            return new TestConnectionResultResource
            {
                Success = false,
                ErrorMessage = "Identity provider not found"
            };
        }

        return await _ldapService.TestConnectionAsync(provider);
    }
}
