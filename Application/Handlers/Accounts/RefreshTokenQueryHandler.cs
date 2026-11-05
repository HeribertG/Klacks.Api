// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class RefreshTokenQueryHandler : IRequestHandler<RefreshTokenQuery, TokenResource?>
{
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    private readonly ILogger<RefreshTokenQueryHandler> _logger;

    public RefreshTokenQueryHandler(
        IAccountAuthenticationService accountAuthenticationService,
        ILogger<RefreshTokenQueryHandler> logger)
    {
        _accountAuthenticationService = accountAuthenticationService;
        _logger = logger;
    }

    public async Task<TokenResource?> Handle(RefreshTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing token refresh request");

            var fromCookie = !string.IsNullOrEmpty(request.CookieRefreshToken);
            var result = await RenewAsync(request);
            if (result != null && result.Success)
            {
                _logger.LogInformation("Token refresh successful for user: {UserId}", result.Id);
                return fromCookie ? ToTokenResource<TokenResource>(result) : ToTokenResource<MigrationTokenResource>(result);
            }

            _logger.LogWarning("Token refresh failed - returning null");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing token refresh");
            throw;
        }
    }

    /// <summary>
    /// Uses the one token of the request. The cookie wins: it renews the session and keeps its token (user taken from the
    /// refresh token alone, no access token). Otherwise a body token is migrated: the old token from local storage was
    /// readable by scripts, so the session gets a new token for the cookie; that answer is a MigrationTokenResource, which
    /// also returns the new token in the body for clients that still run the previous Ui. Returns null without any token.
    /// </summary>
    /// <param name="request">The query with the optional cookie and body tokens</param>
    private async Task<AuthenticatedResult?> RenewAsync(RefreshTokenQuery request)
    {
        if (!string.IsNullOrEmpty(request.CookieRefreshToken))
        {
            return await _accountAuthenticationService.RefreshTokenAsync(
                new RefreshRequestResource { RefreshToken = request.CookieRefreshToken });
        }

        if (!string.IsNullOrEmpty(request.RefreshRequest.RefreshToken))
        {
            return await _accountAuthenticationService.MigrateRefreshTokenAsync(request.RefreshRequest);
        }

        return null;
    }

    private static T ToTokenResource<T>(AuthenticatedResult result)
        where T : TokenResource, new() => new()
    {
        Success = true,
        Token = result.Token,
        Username = result.UserName,
        FirstName = result.FirstName,
        Name = result.Name,
        Id = result.Id,
        ExpTime = result.Expires,
        IsAdmin = result.IsAdmin,
        IsAuthorised = result.IsAuthorised,
        Permissions = result.Permissions,
        RefreshToken = result.RefreshToken,
        Version = MyVersion.Get()
    };
}
