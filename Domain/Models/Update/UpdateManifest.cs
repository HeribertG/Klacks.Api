// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Update;

public class UpdateManifest
{
    public UpdateChannel Channel { get; set; }

    public string LatestVersion { get; set; } = string.Empty;

    public string MinUpgradableFrom { get; set; } = string.Empty;

    public DateTime ReleasedAt { get; set; }

    public bool ContainsMigrations { get; set; }

    public UpdateArtifacts Artifacts { get; set; } = new();

    public string Signature { get; set; } = string.Empty;

    public string? ChangelogUrl { get; set; }
}
