// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Generates Klacks bot tokens using the same CSPRNG + SHA-256 hashing approach as
/// PatTokenGenerator, but with its own prefix so the bot token universe never collides
/// with personal access tokens or ERP import tokens.
/// </summary>
using System.Buffers.Text;
using System.Security.Cryptography;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Security;

public static class KlacksBotTokenGenerator
{
    /// <summary>
    /// Creates a new Klacks bot token. Returns the plaintext token (shown to the caller
    /// exactly once), its SHA-256 hash for persistence and the display prefix.
    /// </summary>
    public static (string Plaintext, string TokenHash, string TokenPrefix) Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(KlacksBotTokenConstants.TokenByteLength);
        var plaintext = KlacksBotTokenConstants.TokenPrefix + Base64Url.EncodeToString(randomBytes);
        var tokenHash = PatTokenGenerator.HashToken(plaintext);
        var tokenPrefix = plaintext[..KlacksBotTokenConstants.DisplayPrefixLength];

        return (plaintext, tokenHash, tokenPrefix);
    }
}
