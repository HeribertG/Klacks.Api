// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for RFC 7591 dynamic client registration. Validates the redirect URIs
/// (absolute, HTTPS or loopback HTTP only) and the requested grant/response types,
/// persists the client and returns the public client metadata for a public client
/// without a client secret.
/// </summary>
/// <param name="request">Contains the registration request with client name and redirect URIs</param>

using System.Text.Json;
using Klacks.Api.Application.Commands.OAuth;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.OAuth;

public class RegisterOAuthClientCommandHandler : IRequestHandler<RegisterOAuthClientCommand, OAuthClientRegistrationResult>
{
    private const string DefaultClientName = "MCP Client";
    private const int MaxClientNameLength = 256;

    private readonly IOAuthClientRepository _repository;

    public RegisterOAuthClientCommandHandler(IOAuthClientRepository repository)
    {
        _repository = repository;
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

        foreach (var redirectUri in registration.RedirectUris)
        {
            if (!IsAcceptableRedirectUri(redirectUri))
            {
                return OAuthClientRegistrationResult.Rejected(
                    OAuthConstants.ErrorInvalidRedirectUri,
                    $"Redirect URI '{redirectUri}' must be an absolute HTTPS URI or a loopback HTTP URI.");
            }
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

        var trimmedClientName = registration.ClientName?.Trim() ?? string.Empty;
        var clientName = trimmedClientName.Length == 0
            ? DefaultClientName
            : trimmedClientName[..Math.Min(trimmedClientName.Length, MaxClientNameLength)];

        var client = new OAuthClient
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid().ToString("N"),
            ClientName = clientName,
            RedirectUrisJson = JsonSerializer.Serialize(registration.RedirectUris)
        };

        await _repository.AddAsync(client, cancellationToken);

        var response = new OAuthClientRegistrationResponse(
            ClientId: client.ClientId,
            ClientIdIssuedAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ClientName: clientName,
            RedirectUris: registration.RedirectUris,
            GrantTypes: [OAuthConstants.GrantTypeAuthorizationCode],
            ResponseTypes: [OAuthConstants.ResponseTypeCode],
            TokenEndpointAuthMethod: OAuthConstants.TokenEndpointAuthMethodNone);

        return OAuthClientRegistrationResult.Success(response);
    }

    private static bool IsAcceptableRedirectUri(string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        return uri.Scheme == Uri.UriSchemeHttp && IsLoopbackHost(uri);
    }

    private static bool IsLoopbackHost(Uri uri)
    {
        return uri.IsLoopback || string.Equals(uri.Host, OAuthConstants.LoopbackHostLocalhost, StringComparison.OrdinalIgnoreCase);
    }
}
