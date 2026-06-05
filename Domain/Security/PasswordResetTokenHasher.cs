// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Domain.Security;

/// <summary>
/// Produces a stable, deterministic hash of a password reset token so that only the
/// hash is persisted and compared, never the raw token.
/// </summary>
public static class PasswordResetTokenHasher
{
    /// <summary>
    /// Returns the uppercase hex-encoded SHA-256 hash of the given token. The token is
    /// high-entropy and random, so no salt is required.
    /// </summary>
    /// <param name="token">The raw password reset token to hash.</param>
    public static string Hash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
