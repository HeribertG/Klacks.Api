// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for revoking a Klacks bot token.
/// </summary>
/// <param name="request">Contains the token id to revoke</param>

using Klacks.Api.Application.Commands.Bots;
using Klacks.Api.Domain.Interfaces.Bots;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Bots;

public class RevokeKlacksBotTokenCommandHandler : IRequestHandler<RevokeKlacksBotTokenCommand, bool>
{
    private readonly IKlacksBotTokenRepository _tokenRepository;

    public RevokeKlacksBotTokenCommandHandler(IKlacksBotTokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }

    public async Task<bool> Handle(RevokeKlacksBotTokenCommand request, CancellationToken cancellationToken)
    {
        var revoked = await _tokenRepository.RevokeAsync(request.Id, cancellationToken);
        return revoked != null;
    }
}
