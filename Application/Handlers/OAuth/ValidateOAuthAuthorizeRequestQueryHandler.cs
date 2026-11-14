// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler validating an OAuth authorization request: client existence, exact redirect URI
/// match against the registration, trust of the effective redirect URI (the same check as at code issue),
/// response type and PKCE S256 requirements. Distinguishes errors that must never be redirected (unknown
/// client, unregistered or untrusted redirect URI) from errors that are reported via redirect to the
/// validated redirect URI. The result carries the trusted-list entry the page shows instead of any client name.
/// </summary>
/// <param name="request">Contains client id, redirect URI, response type and PKCE challenge parameters</param>

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Application.Interfaces.OAuth;
using Klacks.Api.Application.Queries.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.OAuth;

public class ValidateOAuthAuthorizeRequestQueryHandler : IRequestHandler<ValidateOAuthAuthorizeRequestQuery, OAuthAuthorizeValidationResult>
{
    private readonly IOAuthClientRepository _repository;
    private readonly IOAuthRedirectTrustPolicy _trustPolicy;

    public ValidateOAuthAuthorizeRequestQueryHandler(IOAuthClientRepository repository, IOAuthRedirectTrustPolicy trustPolicy)
    {
        _repository = repository;
        _trustPolicy = trustPolicy;
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

        var resolution = await _trustPolicy.ResolveAsync(client, request.RedirectUri, cancellationToken);
        if (!resolution.IsResolved)
        {
            return OAuthAuthorizeValidationResult.RejectedWithoutRedirect(resolution.Error!, resolution.ErrorDescription!);
        }

        var effectiveRedirectUri = resolution.RedirectUri!;

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

        return OAuthAuthorizeValidationResult.Success(resolution.Match!, resolution.Settings.AllowWrite, _trustPolicy.AccessTokenLifetime, effectiveRedirectUri);
    }
}
