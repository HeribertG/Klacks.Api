// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for revoking a personal access token. Revocation is scoped to the owner so that
/// foreign tokens are indistinguishable from missing ones.
/// </summary>
/// <param name="request">Contains the token id and the owner user id</param>

using Klacks.Api.Application.Commands.Authentification;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Authentification;

public class RevokePersonalAccessTokenCommandHandler : IRequestHandler<RevokePersonalAccessTokenCommand, bool>
{
    private readonly IPersonalAccessTokenRepository _repository;

    public RevokePersonalAccessTokenCommandHandler(IPersonalAccessTokenRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(RevokePersonalAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var revoked = await _repository.RevokeAsync(request.Id, request.UserId, cancellationToken);

        return revoked != null;
    }
}
