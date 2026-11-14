// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for the OAuth token endpoint. Consumes the single-use authorization code,
/// verifies the PKCE S256 code verifier and the client binding, and issues a Klacks
/// personal access token as the OAuth access token. The token takes the access mode bound to the code; it is refused
/// with invalid_grant when the redirect target is no longer trusted or write access was switched off after the code was
/// issued. Name, tool name and host of the token come from the trusted-list entry, never from the client registration.
/// The returned scope follows the mode (mcp:tools for write, mcp:tools:read for read). The token lives 30 days, on the
/// Playground 24 hours (IOAuthRedirectTrustPolicy.AccessTokenLifetime).
/// </summary>
/// <param name="request">Contains grant type, authorization code, redirect URI, client id and PKCE verifier</param>

using Klacks.Api.Application.Commands.OAuth;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Application.Interfaces.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Security;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.Handlers.OAuth;

public class ExchangeOAuthTokenCommandHandler : IRequestHandler<ExchangeOAuthTokenCommand, OAuthTokenResult>
{
    private readonly IOAuthClientRepository _clientRepository;
    private readonly IOAuthAuthorizationCodeStore _codeStore;
    private readonly IPersonalAccessTokenRepository _tokenRepository;
    private readonly IOAuthRedirectTrustPolicy _trustPolicy;

    public ExchangeOAuthTokenCommandHandler(
        IOAuthClientRepository clientRepository,
        IOAuthAuthorizationCodeStore codeStore,
        IPersonalAccessTokenRepository tokenRepository,
        IOAuthRedirectTrustPolicy trustPolicy)
    {
        _clientRepository = clientRepository;
        _codeStore = codeStore;
        _tokenRepository = tokenRepository;
        _trustPolicy = trustPolicy;
    }

    public async Task<OAuthTokenResult> Handle(ExchangeOAuthTokenCommand request, CancellationToken cancellationToken)
    {
        if (request.GrantType != OAuthConstants.GrantTypeAuthorizationCode)
        {
            return OAuthTokenResult.Rejected(
                OAuthConstants.ErrorUnsupportedGrantType,
                $"Only the '{OAuthConstants.GrantTypeAuthorizationCode}' grant type is supported.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return OAuthTokenResult.Rejected(OAuthConstants.ErrorInvalidRequest, "The code parameter is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return OAuthTokenResult.Rejected(OAuthConstants.ErrorInvalidRequest, "The client_id parameter is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CodeVerifier))
        {
            return OAuthTokenResult.Rejected(OAuthConstants.ErrorInvalidRequest, "The code_verifier parameter is required (PKCE).");
        }

        var client = await _clientRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client == null)
        {
            return OAuthTokenResult.Rejected(
                OAuthConstants.ErrorInvalidClient,
                "Unknown client.",
                StatusCodes.Status401Unauthorized);
        }

        var codeData = await _codeStore.ConsumeAsync(request.Code, cancellationToken);
        if (codeData == null)
        {
            return OAuthTokenResult.Rejected(OAuthConstants.ErrorInvalidGrant, "Invalid or expired authorization code.");
        }

        if (!string.Equals(codeData.ClientId, request.ClientId, StringComparison.Ordinal))
        {
            return OAuthTokenResult.Rejected(OAuthConstants.ErrorInvalidGrant, "Authorization code was not issued to this client.");
        }

        if (request.RedirectUri != null && !string.Equals(codeData.RedirectUri, request.RedirectUri, StringComparison.Ordinal))
        {
            return OAuthTokenResult.Rejected(OAuthConstants.ErrorInvalidGrant, "Redirect URI mismatch.");
        }

        if (!PkceChallengeValidator.Verify(request.CodeVerifier, codeData.CodeChallenge))
        {
            return OAuthTokenResult.Rejected(OAuthConstants.ErrorInvalidGrant, "Code verifier does not match the challenge.");
        }

        var settings = await _trustPolicy.GetSettingsAsync(cancellationToken);
        var decision = settings.Evaluate(codeData.RedirectUri);
        if (!decision.IsTrusted)
        {
            return OAuthTokenResult.Rejected(OAuthConstants.ErrorInvalidGrant, "The redirect target of this authorization code is no longer trusted.");
        }

        if (codeData.AccessMode == PersonalAccessTokenAccessMode.Write && !settings.AllowWrite)
        {
            return OAuthTokenResult.Rejected(OAuthConstants.ErrorInvalidGrant, "Write access through OAuth is no longer allowed on this installation.");
        }

        var match = decision.Match!;
        var (plaintext, tokenHash, tokenPrefix) = PatTokenGenerator.Generate();
        var lifetime = _trustPolicy.AccessTokenLifetime;

        var token = new PersonalAccessToken
        {
            Id = Guid.NewGuid(),
            UserId = codeData.UserId,
            Name = OAuthConstants.AccessTokenNamePrefix + match.DisplayName,
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            AccessMode = codeData.AccessMode,
            IsOAuthIssued = true,
            OAuthToolName = match.DisplayName,
            OAuthRedirectHost = match.Host
        };

        await _tokenRepository.AddAsync(token, cancellationToken);

        var response = new OAuthTokenResponse(
            AccessToken: plaintext,
            TokenType: OAuthConstants.TokenTypeBearer,
            ExpiresIn: (int)lifetime.TotalSeconds,
            Scope: codeData.AccessMode == PersonalAccessTokenAccessMode.Write ? OAuthConstants.McpToolsScope : OAuthConstants.McpToolsReadScope);

        return OAuthTokenResult.Success(response);
    }
}
