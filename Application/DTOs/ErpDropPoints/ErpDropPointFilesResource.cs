// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpDropPointFilesResource
{
    public List<ErpDropPointFileResource> Pending { get; set; } = [];

    public List<ErpDropPointFileResource> Processed { get; set; } = [];

    public List<ErpDropPointFileResource> Error { get; set; } = [];
}
