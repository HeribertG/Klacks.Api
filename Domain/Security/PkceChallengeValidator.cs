// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates PKCE S256 code verifiers against the stored code challenge (RFC 7636) using
/// a constant-time comparison of the Base64Url-encoded SHA-256 hash.
/// </summary>

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Domain.Security;

public static class PkceChallengeValidator
{
    /// <summary>
    /// Returns true when the Base64Url-encoded SHA-256 hash of the verifier matches the challenge.
    /// </summary>
    /// <param name="codeVerifier">The plaintext PKCE code verifier sent by the client to the token endpoint.</param>
    /// <param name="codeChallenge">The S256 code challenge captured during the authorization request.</param>
    public static bool Verify(string codeVerifier, string codeChallenge)
    {
        var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var computedChallenge = Base64Url.EncodeToString(challengeBytes);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedChallenge),
            Encoding.UTF8.GetBytes(codeChallenge));
    }
}
