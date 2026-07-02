// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Authentication handler for ERP import tokens. Deliberately separate from PatAuthenticationHandler
/// and the KlacksPat scheme: an ERP import token identifies only a drop point, carries no user
/// roles, and is only ever accepted by endpoints that explicitly opt into the KlacksErpImport
/// scheme -- a leaked token cannot authenticate anywhere else in the API (MCP, REST, etc.).
/// </summary>
/// <param name="tokenRepository">Repository used for hash lookup and last-used updates</param>
/// <param name="dropPointRepository">Repository used to reject tokens whose drop point was disabled</param>

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Authentication;

public class ErpImportTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string InvalidTokenMessage = "Invalid or expired ERP import token.";

    private readonly IErpImportTokenRepository _tokenRepository;
    private readonly IErpDropPointRepository _dropPointRepository;

    public ErpImportTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IErpImportTokenRepository tokenRepository,
        IErpDropPointRepository dropPointRepository)
        : base(options, logger, encoder)
    {
        _tokenRepository = tokenRepository;
        _dropPointRepository = dropPointRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawToken = ReadBearerToken();
        if (rawToken == null || !rawToken.StartsWith(ErpImportTokenConstants.TokenPrefix, StringComparison.Ordinal))
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

        var dropPoint = await _dropPointRepository.GetNoTracking(token.DropPointId);
        if (dropPoint == null || dropPoint.IsDeleted || !dropPoint.IsEnabled)
        {
            return AuthenticateResult.Fail(InvalidTokenMessage);
        }

        await _tokenRepository.UpdateLastUsedAsync(token.Id, utcNow, Context.RequestAborted);

        var claims = new List<Claim>
        {
            new(ErpImportTokenConstants.DropPointIdClaimType, token.DropPointId.ToString())
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
