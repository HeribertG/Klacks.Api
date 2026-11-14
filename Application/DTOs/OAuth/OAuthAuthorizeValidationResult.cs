// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.DTOs.OAuth;

public record OAuthAuthorizeValidationResult(
    bool IsValid,
    bool CanRedirectError,
    string? Error,
    string? ErrorDescription,
    OAuthRedirectMatch? Match,
    bool IsWriteAllowed,
    TimeSpan AccessTokenLifetime,
    string? EffectiveRedirectUri)
{
    public static OAuthAuthorizeValidationResult Success(OAuthRedirectMatch match, bool isWriteAllowed, TimeSpan accessTokenLifetime, string redirectUri) =>
        new(true, true, null, null, match, isWriteAllowed, accessTokenLifetime, redirectUri);

    public static OAuthAuthorizeValidationResult RejectedWithoutRedirect(string error, string description) =>
        new(false, false, error, description, null, false, TimeSpan.Zero, null);

    public static OAuthAuthorizeValidationResult RejectedWithRedirect(string error, string description, string redirectUri) =>
        new(false, true, error, description, null, false, TimeSpan.Zero, redirectUri);
}
