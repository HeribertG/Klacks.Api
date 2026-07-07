// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Authentication handler for the Klacks bot token. Deliberately separate from PatAuthenticationHandler
/// and the KlacksErpImport scheme: a bot token identifies only the bot itself, carries no user
/// roles, and is only ever accepted by endpoints that explicitly opt into the KlacksBotToken
/// scheme -- a leaked token cannot authenticate anywhere else in the API (MCP, REST, etc.).
/// </summary>
/// <param name="tokenRepository">Repository used for hash lookup and last-used updates</param>

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Bots;
using Klacks.Api.Domain.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Authentication;

public class KlacksBotTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string InvalidTokenMessage = "Invalid or expired bot token.";
    private const string BotIdentityValue = "otto";

    private readonly IKlacksBotTokenRepository _tokenRepository;

    public KlacksBotTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IKlacksBotTokenRepository tokenRepository)
        : base(options, logger, encoder)
    {
        _tokenRepository = tokenRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawToken = ReadBearerToken();
        if (rawToken == null || !rawToken.StartsWith(KlacksBotTokenConstants.TokenPrefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var tokenHash = PatTokenGenerator.HashToken(rawToken);
        var token = await _tokenRepository.GetByHashAsync(tokenHash, Context.RequestAborted);
        if (token == null || token.IsDeleted)
        {
            return AuthenticateResult.Fail(InvalidTokenMessage);
        }

        var utcNow = DateTime.UtcNow;
        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value <= utcNow)
        {
            return AuthenticateResult.Fail(InvalidTokenMessage);
        }

        await _tokenRepository.UpdateLastUsedAsync(token.Id, utcNow, Context.RequestAborted);

        var claims = new List<Claim>
        {
            new(KlacksBotTokenConstants.BotIdentityClaimType, BotIdentityValue)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private string? ReadBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        if (!AuthenticationHeaderValue.TryParse(header, out var parsed))
        {
            return null;
        }

        if (!string.Equals(parsed.Scheme, JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return parsed.Parameter;
    }
}
