// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for the OAuth token endpoint. Consumes the single-use authorization code,
/// verifies the PKCE S256 code verifier and the client binding, and issues a Klacks
/// personal access token as the OAuth access token.
/// </summary>
/// <param name="request">Contains grant type, authorization code, redirect URI, client id and PKCE verifier</param>

using Klacks.Api.Application.Commands.OAuth;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Domain.Constants;
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

    public ExchangeOAuthTokenCommandHandler(
        IOAuthClientRepository clientRepository,
        IOAuthAuthorizationCodeStore codeStore,
        IPersonalAccessTokenRepository tokenRepository)
    {
        _clientRepository = clientRepository;
        _codeStore = codeStore;
        _tokenRepository = tokenRepository;
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

        var codeData = _codeStore.Consume(request.Code);
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

        var (plaintext, tokenHash, tokenPrefix) = PatTokenGenerator.Generate();
        var lifetime = TimeSpan.FromDays(OAuthConstants.AccessTokenExpiresInDays);

        var token = new PersonalAccessToken
        {
            Id = Guid.NewGuid(),
            UserId = codeData.UserId,
            Name = OAuthConstants.AccessTokenNamePrefix + codeData.ClientName,
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            ExpiresAt = DateTime.UtcNow.Add(lifetime)
        };

        await _tokenRepository.AddAsync(token, cancellationToken);

        var response = new OAuthTokenResponse(
            AccessToken: plaintext,
            TokenType: OAuthConstants.TokenTypeBearer,
            ExpiresIn: (int)lifetime.TotalSeconds,
            Scope: codeData.Scope);

        return OAuthTokenResult.Success(response);
    }
}
