// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Exports;

public class ExportFormatOverrideResource
{
    public string FormatKey { get; set; } = string.Empty;

    public string PatchJson { get; set; } = "{}";

    public bool IsEnabled { get; set; }

    public string? Note { get; set; }

    public string CreatedUnderVersion { get; set; } = string.Empty;

    public DateTime? UpdateTime { get; set; }
}
