// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Exports;

public class ExportFormatOverrideCatalog
{
    public string CurrentVersion { get; set; } = string.Empty;

    public IReadOnlyList<ExportFormatOverrideFormatInfo> Formats { get; set; } = [];
}
