// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lifetime of a session refresh token. Shared by the token store, which stamps the expiry date, and the
/// refresh cookie, whose Max-Age matches it.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class RefreshTokenLifetime
{
    public const int Days = 30;

    public static readonly TimeSpan Duration = TimeSpan.FromDays(Days);

    /// <summary>
    /// How long a refresh token from local storage stays valid after it was exchanged for a new cookie token (migration
    /// to the HttpOnly cookie). The old token was readable by scripts, so it is not kept as the session token; it is
    /// shortened instead of deleted, so several tabs migrating at the same moment with the same old token all succeed
    /// instead of one of them getting 401 and logging out.
    /// </summary>
    public static readonly TimeSpan LegacyTokenGrace = TimeSpan.FromMinutes(2);
}
