// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="LogoutCommand"/>. Removes only the presented refresh token, so logging out ends this device's
/// session and leaves the user's other devices signed in. Without a token it does nothing.
/// </summary>
/// <param name="request">Contains the refresh token of this device, optional</param>

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
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            await _refreshTokenService.RemoveRefreshTokenAsync(request.RefreshToken);
        }

        return Unit.Value;
    }
}
