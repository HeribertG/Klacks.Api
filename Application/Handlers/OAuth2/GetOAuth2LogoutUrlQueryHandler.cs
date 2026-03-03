// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.OAuth2;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.OAuth2;

namespace Klacks.Api.Application.Handlers.OAuth2;

public class GetOAuth2LogoutUrlQueryHandler : IRequestHandler<GetOAuth2LogoutUrlQuery, OAuth2LogoutUrlResponse>
{
    private readonly IIdentityProviderRepository _providerRepository;
    private readonly ILogger<GetOAuth2LogoutUrlQueryHandler> _logger;

    public GetOAuth2LogoutUrlQueryHandler(
        IIdentityProviderRepository providerRepository,
        ILogger<GetOAuth2LogoutUrlQueryHandler> logger)
    {
        _providerRepository = providerRepository;
        _logger = logger;
    }

    public async Task<OAuth2LogoutUrlResponse> Handle(GetOAuth2LogoutUrlQuery request, CancellationToken cancellationToken)
    {
        var provider = await _providerRepository.Get(request.ProviderId);
        if (provider == null)
        {
            throw new NotFoundException("Provider not found");
        }

        var logoutUrl = GenerateLogoutUrl(provider, request.PostLogoutRedirectUri);

        return new OAuth2LogoutUrlResponse
        {
            LogoutUrl = logoutUrl,
            SupportsLogout = !string.IsNullOrEmpty(logoutUrl)
        };
    }

    private string? GenerateLogoutUrl(IdentityProvider provider, string? postLogoutRedirectUri)
    {
        if (string.IsNullOrEmpty(provider.Host))
        {
            return null;
        }

        var protocol = provider.UseSsl ? "https" : "http";
        var port = provider.Port.HasValue && provider.Port.Value != 443 && provider.Port.Value != 80
            ? $":{provider.Port.Value}"
            : "";

        if (provider.Type == IdentityProviderType.OpenIdConnect &&
            (provider.AuthorizationUrl?.Contains("SSOOauth", StringComparison.OrdinalIgnoreCase) == true ||
             provider.AuthorizationUrl?.Contains("/sso/webman/", StringComparison.OrdinalIgnoreCase) == true))
        {
            _logger.LogInformation("[OAUTH2] Synology SSO does not support external logout");
            return null;
        }

        string logoutUrl;

        if (provider.Type == IdentityProviderType.OpenIdConnect)
        {
            var baseUrl = provider.AuthorizationUrl?.Replace("/authorize", "").TrimEnd('/');
            logoutUrl = $"{baseUrl}/logout";
        }
        else
        {
            return null;
        }

        if (!string.IsNullOrEmpty(postLogoutRedirectUri))
        {
            logoutUrl += $"?redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";
        }

        _logger.LogInformation("[OAUTH2] Generated logout URL: {LogoutUrl}", logoutUrl);
        return logoutUrl;
    }
}
