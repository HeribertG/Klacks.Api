// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IOAuth2Service
{
    string GetAuthorizationUrl(IdentityProvider provider, string redirectUri, string state);
    Task<OAuth2TokenResponse?> ExchangeCodeForTokenAsync(IdentityProvider provider, string code, string redirectUri);
    Task<OAuth2UserInfo?> GetUserInfoAsync(IdentityProvider provider, string accessToken);
}
