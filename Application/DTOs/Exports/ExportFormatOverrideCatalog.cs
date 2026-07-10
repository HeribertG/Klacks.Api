// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Exports;

public class ExportFormatOverrideCatalog
{
    public string CurrentVersion { get; set; } = string.Empty;

    public IReadOnlyList<ExportFormatOverrideFormatInfo> Formats { get; set; } = [];
}
