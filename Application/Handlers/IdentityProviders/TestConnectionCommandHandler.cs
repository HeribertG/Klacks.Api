// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class TestConnectionCommandHandler : IRequestHandler<TestConnectionCommand, TestConnectionResultResource>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly ILogger<TestConnectionCommandHandler> _logger;

    public TestConnectionCommandHandler(
        IIdentityProviderRepository repository,
        ILogger<TestConnectionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TestConnectionResultResource> Handle(TestConnectionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing connection for identity provider: {Id}", request.Id);

        return await _repository.TestConnectionAsync(request.Id);
    }
}
