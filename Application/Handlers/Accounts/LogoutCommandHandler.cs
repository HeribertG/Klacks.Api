// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="LogoutCommand"/>. Removes all refresh tokens of the requesting user so
/// a copy of the refresh token (e.g. stolen via XSS or a lost device) can no longer be redeemed.
/// </summary>
/// <param name="request">Contains the id of the user to log out</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokenService.RemoveAllUserRefreshTokensAsync(request.UserId);
        return Unit.Value;
    }
}
