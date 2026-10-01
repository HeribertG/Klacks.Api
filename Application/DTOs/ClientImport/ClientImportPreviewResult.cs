// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportPreviewResult
{
    public List<ClientImportPreviewRow> Rows { get; set; } = [];

    public ClientImportSummary Summary { get; set; } = new();

    public List<int> UnmappedColumns { get; set; } = [];

    public List<ClientImportTarget> IgnoredTargets { get; set; } = [];
}
