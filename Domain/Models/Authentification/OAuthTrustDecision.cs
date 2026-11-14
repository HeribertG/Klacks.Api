// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Authentification;

/// <summary>
/// Result of checking one redirect URI against the trust settings.
/// </summary>
/// <param name="Outcome">Trusted or the reason the target is refused</param>
/// <param name="Match">The matched entry when trusted, otherwise null</param>
/// <param name="Target">Short description of the refused target (scheme and authority), empty when trusted</param>
public sealed record OAuthTrustDecision(OAuthTrustOutcome Outcome, OAuthRedirectMatch? Match, string Target)
{
    public bool IsTrusted => Outcome == OAuthTrustOutcome.Trusted;
}
