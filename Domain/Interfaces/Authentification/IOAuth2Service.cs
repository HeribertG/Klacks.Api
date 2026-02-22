// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IOAuth2Service
{
    string GetAuthorizationUrl(IdentityProvider provider, string redirectUri, string state);
    Task<OAuth2TokenResponse?> ExchangeCodeForTokenAsync(IdentityProvider provider, string code, string redirectUri);
    Task<OAuth2UserInfo?> GetUserInfoAsync(IdentityProvider provider, string accessToken);
}

public class OAuth2TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; }
    public int? ExpiresIn { get; set; }
    public string? IdToken { get; set; }
}

public class OAuth2UserInfo
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Picture { get; set; }
}
