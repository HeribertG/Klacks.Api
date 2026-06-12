// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Authentication handler for personal access tokens. Validates bearer tokens carrying the
/// well-known PAT prefix via hash lookup, mirrors the login JWT claims for the token owner
/// and throttles last-used tracking; all other tokens fall through to the JWT handler.
/// </summary>
/// <param name="tokenRepository">Repository used for hash lookup and last-used updates</param>
/// <param name="userManager">Identity user manager used to load the token owner and roles</param>

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Klacks.Api.Infrastructure.Authentication;

public class PatAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string InvalidTokenMessage = "Invalid or expired personal access token.";

    private readonly IPersonalAccessTokenRepository _tokenRepository;
    private readonly UserManager<AppUser> _userManager;

    public PatAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IPersonalAccessTokenRepository tokenRepository,
        UserManager<AppUser> userManager)
        : base(options, logger, encoder)
    {
        _tokenRepository = tokenRepository;
        _userManager = userManager;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawToken = ReadBearerToken();
        if (rawToken == null || !rawToken.StartsWith(PatConstants.TokenPrefix, StringComparison.Ordinal))
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

        var user = await _userManager.FindByIdAsync(token.UserId);
        if (user == null)
        {
            return AuthenticateResult.Fail(InvalidTokenMessage);
        }

        await UpdateLastUsedIfStaleAsync(token, utcNow);

        var principal = await BuildPrincipalAsync(user, utcNow);

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

    private async Task UpdateLastUsedIfStaleAsync(PersonalAccessToken token, DateTime utcNow)
    {
        if (token.LastUsedAt.HasValue && utcNow - token.LastUsedAt.Value < PatConstants.LastUsedUpdateInterval)
        {
            return;
        }

        await _tokenRepository.UpdateLastUsedAsync(token.Id, utcNow, Context.RequestAborted);
    }

    private async Task<ClaimsPrincipal> BuildPrincipalAsync(AppUser user, DateTime utcNow)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(utcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);

        return new ClaimsPrincipal(identity);
    }
}
