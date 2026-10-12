// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Artifacts of one release: the Docker images and the native bundles per runtime identifier.
/// OnPremWindows is the original name of the native list and is still read for older manifests.
/// </summary>

namespace Klacks.Api.Domain.Models.Update;

public class UpdateArtifacts
{
    public DockerArtifact? Docker { get; set; }

    public List<OnPremArtifact> Native { get; set; } = new();

    public List<OnPremArtifact> OnPremWindows { get; set; } = new();
}
