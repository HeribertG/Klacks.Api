// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.OAuth;

public record OAuthAuthorizeValidationResult(
    bool IsValid,
    bool CanRedirectError,
    string? Error,
    string? ErrorDescription,
    string? ClientName,
    string? EffectiveRedirectUri)
{
    public static OAuthAuthorizeValidationResult Success(string clientName, string redirectUri) =>
        new(true, true, null, null, clientName, redirectUri);

    public static OAuthAuthorizeValidationResult RejectedWithoutRedirect(string error, string description) =>
        new(false, false, error, description, null, null);

    public static OAuthAuthorizeValidationResult RejectedWithRedirect(string error, string description, string redirectUri) =>
        new(false, true, error, description, null, redirectUri);
}
