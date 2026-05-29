// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Update;

public class UpdateArtifacts
{
    public DockerArtifact? Docker { get; set; }

    public List<OnPremArtifact> OnPremWindows { get; set; } = new();
}
