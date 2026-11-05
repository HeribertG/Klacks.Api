// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lifetime of an access JWT. Shared by the token issuer, which stamps the expiry, and the access token cookie, whose
/// Max-Age matches it.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class AccessTokenLifetime
{
    public const int Minutes = 15;

    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(Minutes);
}
