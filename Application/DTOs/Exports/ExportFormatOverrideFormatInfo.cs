// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Exports;

public class ExportFormatOverrideFormatInfo
{
    public string FormatKey { get; set; } = string.Empty;

    public string Family { get; set; } = string.Empty;

    public IReadOnlyList<string> AllowedKeys { get; set; } = [];

    public ExportFormatOverrideResource? Override { get; set; }
}
