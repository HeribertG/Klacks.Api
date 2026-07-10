// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Exports;

public class ExportPreviewResult
{
    public byte[] FileContent { get; set; } = [];

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public bool OverrideApplied { get; set; }
}
