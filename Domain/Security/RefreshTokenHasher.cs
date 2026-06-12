// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Produces a stable, deterministic hash of a refresh token so that only the hash is
/// persisted and compared, never the raw token. A leaked database therefore cannot be
/// used to impersonate sessions.
/// </summary>

using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Domain.Security;

public static class RefreshTokenHasher
{
    /// <summary>
    /// Returns the uppercase hex-encoded SHA-256 hash of the given token. The token is
    /// high-entropy and random, so no salt is required.
    /// </summary>
    /// <param name="token">The raw refresh token to hash.</param>
    public static string Hash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
