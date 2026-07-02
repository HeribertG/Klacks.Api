// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Generates ERP import tokens using the same CSPRNG + SHA-256 hashing approach as
/// PatTokenGenerator, but with its own prefix so the two token universes never collide.
/// </summary>
using System.Buffers.Text;
using System.Security.Cryptography;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Security;

public static class ErpImportTokenGenerator
{
    /// <summary>
    /// Creates a new ERP import token. Returns the plaintext token (shown to the user
    /// exactly once), its SHA-256 hash for persistence and the display prefix.
    /// </summary>
    public static (string Plaintext, string TokenHash, string TokenPrefix) Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(ErpImportTokenConstants.TokenByteLength);
        var plaintext = ErpImportTokenConstants.TokenPrefix + Base64Url.EncodeToString(randomBytes);
        var tokenHash = PatTokenGenerator.HashToken(plaintext);
        var tokenPrefix = plaintext[..ErpImportTokenConstants.DisplayPrefixLength];

        return (plaintext, tokenHash, tokenPrefix);
    }
}
