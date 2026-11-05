// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpDropFolderResource
{
    public string Installation { get; set; } = string.Empty;

    public string FolderPath { get; set; } = string.Empty;

    public string? ContainerPath { get; set; }

    public bool IsHostPath { get; set; }

    public string FolderDropRequirement { get; set; } = string.Empty;

    public List<string> RecommendedChannels { get; set; } = [];
}
