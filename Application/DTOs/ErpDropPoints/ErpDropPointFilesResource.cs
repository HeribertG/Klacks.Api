// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpDropPointFilesResource
{
    public List<ErpImportFileResource> Inbox { get; set; } = [];

    public List<ErpImportFileResource> Processed { get; set; } = [];

    public List<ErpImportFileResource> Failed { get; set; } = [];
}
