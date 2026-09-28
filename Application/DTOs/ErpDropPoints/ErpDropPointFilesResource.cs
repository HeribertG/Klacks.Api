// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpDropPointFilesResource
{
    public List<ErpDropPointFileResource> Pending { get; set; } = [];

    public List<ErpDropPointFileResource> Processed { get; set; } = [];

    public List<ErpDropPointFileResource> Error { get; set; } = [];
}
