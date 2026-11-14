// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for RFC 7591 dynamic client registration. Keeps only the redirect URIs the installation trusts (the shipped
/// list, allowed local programs and administrator targets), drops every other one individually and rejects the
/// registration with invalid_redirect_uri naming the refused targets when none remains. The client name sent by the
/// caller is ignored and not stored: the echoed name is the display name of the first trusted entry. Returns the public
/// client metadata for a public client without a client secret and with only the kept redirect URIs.
/// </summary>
/// <param name="request">Contains the registration request with the redirect URIs and the requested grant and response types</param>

using System.Text.Json;
using Klacks.Api.Application.Commands.OAuth;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Application.Interfaces.OAuth;
using Klacks.Api.Application.Services.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.OAuth;

public class RegisterOAuthClientCommandHandler : IRequestHandler<RegisterOAuthClientCommand, OAuthClientRegistrationResult>
{
    private const int MaxRejectedTargetsInMessage = 5;

    private const string RejectedTargetSeparator = "', '";

    private readonly IOAuthClientRepository _repository;
    private readonly IOAuthRedirectTrustPolicy _trustPolicy;
    private readonly ILogger<RegisterOAuthClientCommandHandler> _logger;

    public RegisterOAuthClientCommandHandler(
        IOAuthClientRepository repository,
        IOAuthRedirectTrustPolicy trustPolicy,
        ILogger<RegisterOAuthClientCommandHandler> logger)
    {
        _repository = repository;
        _trustPolicy = trustPolicy;
        _logger = logger;
    }

    public async Task<OAuthClientRegistrationResult> Handle(RegisterOAuthClientCommand request, CancellationToken cancellationToken)
    {
        var registration = request.Request;

        if (registration.RedirectUris == null || registration.RedirectUris.Count == 0)
        {
            return OAuthClientRegistrationResult.Rejected(
                OAuthConstants.ErrorInvalidRedirectUri,
                "At least one redirect URI is required.");
        }

        if (registration.GrantTypes is { Count: > 0 } && !registration.GrantTypes.Contains(OAuthConstants.GrantTypeAuthorizationCode))
        {
            return OAuthClientRegistrationResult.Rejected(
                OAuthConstants.ErrorInvalidClientMetadata,
                $"Only the '{OAuthConstants.GrantTypeAuthorizationCode}' grant type is supported.");
        }

        if (registration.ResponseTypes is { Count: > 0 } && !registration.ResponseTypes.Contains(OAuthConstants.ResponseTypeCode))
        {
            return OAuthClientRegistrationResult.Rejected(
                OAuthConstants.ErrorInvalidClientMetadata,
                $"Only the '{OAuthConstants.ResponseTypeCode}' response type is supported.");
        }

        if (registration.TokenEndpointAuthMethod != null && registration.TokenEndpointAuthMethod != OAuthConstants.TokenEndpointAuthMethodNone)
        {
            return OAuthClientRegistrationResult.Rejected(
                OAuthConstants.ErrorInvalidClientMetadata,
                $"Only the '{OAuthConstants.TokenEndpointAuthMethodNone}' token endpoint auth method is supported.");
        }

        var settings = await _trustPolicy.GetSettingsAsync(cancellationToken);
        if (settings.Mode == OAuthConnectionMode.Off)
        {
            return OAuthClientRegistrationResult.Forbidden(
                OAuthConstants.ErrorAccessDenied,
                OAuthTrustMessages.ConnectionsDisabledDescription);
        }

        var keptUris = new List<string>();
        var rejected = new List<OAuthTrustDecision>();
        OAuthRedirectMatch? firstMatch = null;

        foreach (var redirectUri in registration.RedirectUris)
        {
            var decision = settings.Evaluate(redirectUri);
            if (decision.IsTrusted)
            {
                if (!keptUris.Contains(redirectUri, StringComparer.Ordinal))
                {
                    keptUris.Add(redirectUri);
                }

                firstMatch ??= decision.Match;
            }
            else
            {
                rejected.Add(decision);
                _logger.LogInformation(
                    "Rejected OAuth redirect target {Target} ({Outcome}) at client registration",
                    decision.Target.ForLog(),
                    decision.Outcome);
            }
        }

        if (firstMatch == null)
        {
            return OAuthClientRegistrationResult.Rejected(
                OAuthConstants.ErrorInvalidRedirectUri,
                DescribeRejection(rejected));
        }

        var client = new OAuthClient
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid().ToString("N"),
            RedirectUrisJson = JsonSerializer.Serialize(keptUris)
        };

        await _repository.AddAsync(client, cancellationToken);

        var response = new OAuthClientRegistrationResponse(
            ClientId: client.ClientId,
            ClientIdIssuedAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ClientName: firstMatch.DisplayName,
            RedirectUris: keptUris,
            GrantTypes: [OAuthConstants.GrantTypeAuthorizationCode],
            ResponseTypes: [OAuthConstants.ResponseTypeCode],
            TokenEndpointAuthMethod: OAuthConstants.TokenEndpointAuthMethodNone);

        return OAuthClientRegistrationResult.Success(response);
    }

    private static string DescribeRejection(IReadOnlyList<OAuthTrustDecision> rejected)
    {
        if (rejected.All(decision => decision.Outcome == OAuthTrustOutcome.LocalProgramsDisabled))
        {
            return OAuthTrustMessages.LocalProgramsDisabledDescription;
        }

        var targets = rejected
            .Select(decision => decision.Target)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxRejectedTargetsInMessage);

        return OAuthTrustMessages.NotTrustedDescription(string.Join(RejectedTargetSeparator, targets));
    }
}
