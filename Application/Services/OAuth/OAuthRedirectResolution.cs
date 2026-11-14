// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Services.OAuth;

/// <summary>
/// Outcome of resolving the redirect URI of an authorization request.
/// </summary>
/// <param name="RedirectUri">Effective redirect URI when resolved</param>
/// <param name="Match">Trusted-list entry of the effective redirect URI when resolved</param>
/// <param name="Settings">Settings the decision was made with</param>
/// <param name="Error">OAuth error code when refused, otherwise null</param>
/// <param name="ErrorDescription">Human readable reason when refused, otherwise null</param>
public sealed record OAuthRedirectResolution(
    string? RedirectUri,
    OAuthRedirectMatch? Match,
    OAuthTrustSettings Settings,
    string? Error,
    string? ErrorDescription)
{
    public bool IsResolved => Error == null;

    public static OAuthRedirectResolution Resolved(string redirectUri, OAuthRedirectMatch match, OAuthTrustSettings settings) =>
        new(redirectUri, match, settings, null, null);

    public static OAuthRedirectResolution Rejected(OAuthTrustSettings settings, string error, string description) =>
        new(null, null, settings, error, description);
}
