// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The signed PostgreSQL engine image of a release for Docker installations.
/// </summary>
/// <param name="Image">Image reference, e.g. ghcr.io/heribertg/klacks-postgres:19</param>
/// <param name="Sha256">Image index digest including the sha256: prefix, exactly as signed</param>
/// <param name="Signature">Base64 RSA-SHA256 signature over "klacks-postgres-engine\n{major}\n{image}\n{sha256}"</param>

namespace Klacks.Api.Domain.Models.Update;

public class PostgresEngineDockerArtifact
{
    public string Image { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;
}
