// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Update;

public class OnPremArtifact
{
    public string Rid { get; set; } = string.Empty;

    public string BundleUrl { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;
}
