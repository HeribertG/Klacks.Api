// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rebuilds the statement Klacks.Marketplace signs for language packs and feature plugins:
/// "{artifactType}\n{name}\n{sha256hex}" (UTF-8, name lower-case, SHA-256 of the exact response bytes as
/// lower-case hex). Verifying this statement instead of the raw bytes binds the signature to the requested
/// artifact, so a signed plugin A cannot be served as plugin B and one language pack cannot be served as
/// another. Region packages keep the raw-bytes signature. Must stay byte-identical to the marketplace side
/// (ArtifactSignatureStatement); both repositories pin the format with the same literal test.
/// </summary>
using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Infrastructure.Services.Settings;

public static class PackageSignatureStatement
{
    private const char Separator = '\n';

    public static byte[] Build(string artifactType, string name, byte[] payload)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(payload));
        var statement = string.Concat(artifactType, Separator, name.ToLowerInvariant(), Separator, hash);
        return Encoding.UTF8.GetBytes(statement);
    }
}
