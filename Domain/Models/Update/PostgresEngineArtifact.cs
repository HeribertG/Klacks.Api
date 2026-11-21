// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The signed PostgreSQL engine a release offers for the engine upgrade: the target major and the packages per platform.
/// </summary>
/// <param name="Major">Target PostgreSQL major, e.g. 19; becomes target_version of the upgrade row</param>
/// <param name="Version">Full engine version, e.g. 19.0 (informational only)</param>
/// <param name="Pgvector">Bundled pgvector version (informational only)</param>
/// <param name="Docker">The engine image for Docker installations</param>
/// <param name="Native">The engine bundles of native installations, one per runtime identifier</param>

namespace Klacks.Api.Domain.Models.Update;

public class PostgresEngineArtifact
{
    public int Major { get; set; }

    public string Version { get; set; } = string.Empty;

    public string Pgvector { get; set; } = string.Empty;

    public PostgresEngineDockerArtifact? Docker { get; set; }

    public List<OnPremArtifact> Native { get; set; } = new();
}
