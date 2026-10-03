// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the access mode (Read/Write) of an authenticated MCP caller. A personal access token
/// carries its mode as a claim; a missing or unparseable claim on a principal authenticated by the
/// personal access token scheme resolves to Read (fail-closed). A login JWT carries no mode claim and
/// keeps full tool use (Write), as before access modes existed. Only the exact enum name is accepted;
/// numeric or differently cased values resolve to Read. The claim is searched across all
/// identities, because the multi-scheme MCP policy merges identities and McpPrincipalCapper rebuilds them.
/// </summary>
/// <param name="user">Claims principal of the MCP request; null resolves to Read</param>

using System.Security.Claims;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Presentation.Mcp;

public static class McpAccessModeResolver
{
    public static PersonalAccessTokenAccessMode Resolve(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return PersonalAccessTokenAccessMode.Read;
        }

        var claim = user.FindFirst(PatConstants.AccessModeClaimType);
        if (claim != null)
        {
            return Enum.TryParse<PersonalAccessTokenAccessMode>(claim.Value, ignoreCase: false, out var mode)
                   && Enum.IsDefined(mode)
                   && string.Equals(mode.ToString(), claim.Value, StringComparison.Ordinal)
                ? mode
                : PersonalAccessTokenAccessMode.Read;
        }

        var authenticatedByPat = user.Identities.Any(identity =>
            string.Equals(identity.AuthenticationType, PatConstants.SchemeName, StringComparison.Ordinal));

        return authenticatedByPat ? PersonalAccessTokenAccessMode.Read : PersonalAccessTokenAccessMode.Write;
    }
}
