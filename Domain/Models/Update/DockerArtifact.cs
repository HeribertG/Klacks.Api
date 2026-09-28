// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Update;

public class DockerArtifact
{
    public string ApiImage { get; set; } = string.Empty;

    public string UiImage { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;
}
