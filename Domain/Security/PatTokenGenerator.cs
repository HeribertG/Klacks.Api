// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Generates personal access tokens and produces a stable, deterministic hash so that
/// only the hash is persisted and compared, never the raw token.
/// </summary>

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Security;

public static class PatTokenGenerator
{
    /// <summary>
    /// Creates a new personal access token consisting of the well-known prefix followed
    /// by Base64Url-encoded CSPRNG bytes. Returns the plaintext token (shown to the user
    /// exactly once), its SHA-256 hash for persistence and the display prefix.
    /// </summary>
    public static (string Plaintext, string TokenHash, string TokenPrefix) Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(PatConstants.TokenByteLength);
        var plaintext = PatConstants.TokenPrefix + Base64Url.EncodeToString(randomBytes);
        var tokenHash = HashToken(plaintext);
        var tokenPrefix = plaintext[..PatConstants.DisplayPrefixLength];

        return (plaintext, tokenHash, tokenPrefix);
    }

    /// <summary>
    /// Returns the lowercase hex-encoded SHA-256 hash of the given token. The token is
    /// high-entropy and random, so no salt is required.
    /// </summary>
    /// <param name="token">The raw personal access token to hash.</param>
    public static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(hash);
    }
}
