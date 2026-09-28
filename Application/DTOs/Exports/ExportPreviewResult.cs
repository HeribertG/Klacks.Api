// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Exports;

public class ExportPreviewResult
{
    public byte[] FileContent { get; set; } = [];

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public bool OverrideApplied { get; set; }
}
