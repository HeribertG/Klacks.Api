// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// JwtBearer events of the API. Token sources, in order:
/// - SignalR hubs (/hubs): the access_token query, then the Authorization header;
/// - the STT WebSocket: the access_token query;
/// - everywhere else the handler reads the Authorization header itself.
/// Only when a request has no Authorization header at all and no query token was taken does the klacks_access cookie
/// supply the token (the header always wins, also an invalid one). A token from the cookie is recorded, and confirmed
/// once it validated, so AccessCookieRequestGuardMiddleware can guard cookie-authenticated requests.
/// </summary>
/// <param name="accessTokenCookie">Reads the klacks_access cookie</param>
/// <param name="logger">Diagnostics of token selection and failed authentications</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;

namespace Klacks.Api.Presentation.Authentication;

public sealed class KlacksJwtBearerEvents : JwtBearerEvents
{
    private const string SttStreamPath = "/api/backend/assistant/stt/stream";
    private const string AccessTokenQueryKey = "access_token";
    private const string BearerPrefix = "Bearer ";

    private readonly IAccessTokenCookieService _accessTokenCookie;
    private readonly ILogger<KlacksJwtBearerEvents> _logger;

    public KlacksJwtBearerEvents(IAccessTokenCookieService accessTokenCookie, ILogger<KlacksJwtBearerEvents> logger)
    {
        _accessTokenCookie = accessTokenCookie;
        _logger = logger;
    }

    public override Task MessageReceived(MessageReceivedContext context)
    {
        var request = context.Request;
        var path = request.Path;
        var queryToken = request.Query[AccessTokenQueryKey];
        var authHeader = request.Headers[HeaderNames.Authorization].FirstOrDefault();
        var isHubPath = path.StartsWithSegments(HubRouteConstants.HubPathPrefix);
        var isSttPath = path.StartsWithSegments(SttStreamPath);

        if (isHubPath)
        {
            if (!string.IsNullOrEmpty(queryToken))
            {
                context.Token = Uri.UnescapeDataString(queryToken.ToString());
            }
            else if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith(BearerPrefix, StringComparison.Ordinal))
            {
                context.Token = authHeader.Substring(BearerPrefix.Length).Trim();
            }
        }
        else if (isSttPath && !string.IsNullOrEmpty(queryToken))
        {
            context.Token = Uri.UnescapeDataString(queryToken.ToString());
        }

        if (context.Token == null && string.IsNullOrEmpty(authHeader))
        {
            var cookieToken = _accessTokenCookie.Read(request);
            if (cookieToken != null)
            {
                context.Token = cookieToken;
                AccessTokenCookieAuthentication.MarkTokenFromCookie(context.HttpContext);
            }
        }

        if (isHubPath && context.Token == null)
        {
            _logger.LogWarning("No token found for hub path. QueryToken={HasQueryToken}, AuthHeader={HasAuthHeader}",
                !string.IsNullOrEmpty(queryToken), !string.IsNullOrEmpty(authHeader));
        }

        return Task.CompletedTask;
    }

    public override Task TokenValidated(TokenValidatedContext context)
    {
        AccessTokenCookieAuthentication.ConfirmIfTokenFromCookie(context.HttpContext);
        return Task.CompletedTask;
    }

    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        _logger.LogWarning("Authentication failed: Path={Path}, Error={Error}",
            context.HttpContext.Request.Path.ToString().ForLog(), context.Exception.Message.ForLog());
        return Task.CompletedTask;
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        _logger.LogDebug("OnChallenge: Path={Path}, Error={Error}, ErrorDescription={ErrorDescription}",
            context.HttpContext.Request.Path.ToString().ForLog(), context.Error.ForLog(), context.ErrorDescription.ForLog());
        return Task.CompletedTask;
    }
}
