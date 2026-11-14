// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Maps a refused trust decision to the OAuth error code and the English description returned to the client.
/// Registration, authorization and code issue all use it, so they refuse the same target with the same words.
/// </summary>
/// <param name="decision">The refused trust decision</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Services.OAuth;

public static class OAuthTrustMessages
{
    public const string ConnectionsDisabledDescription =
        "External tool connections are switched off on this Klacks installation.";

    public const string LocalProgramsDisabledDescription =
        "Connections from programs on this computer are switched off on this Klacks installation. An administrator can allow them.";

    public static (string Error, string Description) For(OAuthTrustDecision decision)
    {
        return decision.Outcome switch
        {
            OAuthTrustOutcome.ConnectionsDisabled => (OAuthConstants.ErrorAccessDenied, ConnectionsDisabledDescription),
            OAuthTrustOutcome.LocalProgramsDisabled => (OAuthConstants.ErrorAccessDenied, LocalProgramsDisabledDescription),
            _ => (OAuthConstants.ErrorInvalidRequest, NotTrustedDescription(decision.Target))
        };
    }

    public static string NotTrustedDescription(string target)
    {
        return $"Redirect target '{target}' is not trusted by this Klacks installation. An administrator can allow it in the OAuth connection settings.";
    }
}
