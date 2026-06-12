// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler validating an OAuth authorization request: client existence, exact redirect URI
/// match against the registration, response type and PKCE S256 requirements. Distinguishes
/// errors that must never be redirected (unknown client, unregistered redirect URI) from
/// errors that are reported via redirect to the validated redirect URI.
/// </summary>
/// <param name="request">Contains client id, redirect URI, response type and PKCE challenge parameters</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Application.Queries.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.OAuth;

public class ValidateOAuthAuthorizeRequestQueryHandler : IRequestHandler<ValidateOAuthAuthorizeRequestQuery, OAuthAuthorizeValidationResult>
{
    private readonly IOAuthClientRepository _repository;

    public ValidateOAuthAuthorizeRequestQueryHandler(IOAuthClientRepository repository)
    {
        _repository = repository;
    }

    public async Task<OAuthAuthorizeValidationResult> Handle(ValidateOAuthAuthorizeRequestQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return OAuthAuthorizeValidationResult.RejectedWithoutRedirect(
                OAuthConstants.ErrorInvalidRequest,
                "The client_id parameter is required.");
        }

        var client = await _repository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client == null)
        {
            return OAuthAuthorizeValidationResult.RejectedWithoutRedirect(
                OAuthConstants.ErrorInvalidClient,
                "Unknown client.");
        }

        var registeredUris = JsonSerializer.Deserialize<List<string>>(client.RedirectUrisJson) ?? [];

        string effectiveRedirectUri;
        if (string.IsNullOrEmpty(request.RedirectUri))
        {
            if (registeredUris.Count != 1)
            {
                return OAuthAuthorizeValidationResult.RejectedWithoutRedirect(
                    OAuthConstants.ErrorInvalidRequest,
                    "The redirect_uri parameter is required.");
            }

            effectiveRedirectUri = registeredUris[0];
        }
        else
        {
            if (!registeredUris.Contains(request.RedirectUri, StringComparer.Ordinal))
            {
                return OAuthAuthorizeValidationResult.RejectedWithoutRedirect(
                    OAuthConstants.ErrorInvalidRequest,
                    "The redirect_uri is not registered for this client.");
            }

            effectiveRedirectUri = request.RedirectUri;
        }

        if (request.ResponseType != OAuthConstants.ResponseTypeCode)
        {
            return OAuthAuthorizeValidationResult.RejectedWithRedirect(
                OAuthConstants.ErrorUnsupportedResponseType,
                $"Only the '{OAuthConstants.ResponseTypeCode}' response type is supported.",
                effectiveRedirectUri);
        }

        if (string.IsNullOrWhiteSpace(request.CodeChallenge))
        {
            return OAuthAuthorizeValidationResult.RejectedWithRedirect(
                OAuthConstants.ErrorInvalidRequest,
                "The code_challenge parameter is required (PKCE).",
                effectiveRedirectUri);
        }

        if (request.CodeChallengeMethod != OAuthConstants.CodeChallengeMethodS256)
        {
            return OAuthAuthorizeValidationResult.RejectedWithRedirect(
                OAuthConstants.ErrorInvalidRequest,
                $"Only the '{OAuthConstants.CodeChallengeMethodS256}' code challenge method is supported.",
                effectiveRedirectUri);
        }

        return OAuthAuthorizeValidationResult.Success(client.ClientName, effectiveRedirectUri);
    }
}
