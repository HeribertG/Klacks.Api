// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source for turning the caller's role claims into the rights list the assistant paths expect.
/// The JWT carries roles only, never granular permissions, so every entry point has to expand them the
/// same way — this replaces the copies that had drifted apart, one of which skipped the expansion and
/// silently locked non-admins out of the skill API.
/// </summary>

using System.Security.Claims;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Presentation.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <param name="user">The authenticated caller</param>
    public static List<string> GetUserRights(this ClaimsPrincipal user)
    {
        return Permissions.ExpandRoles(user.FindAll(ClaimTypes.Role).Select(claim => claim.Value));
    }
}
