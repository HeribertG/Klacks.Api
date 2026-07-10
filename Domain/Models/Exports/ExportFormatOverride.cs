// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Support hotfix row for an export format: a JSON patch of whitelisted parameters (delimiter,
/// encoding, date format, ...) that is applied over the formatter's defaults at export time.
/// CreatedUnderVersion records the app version the patch was written for, so the UI can warn when
/// a later release may have made the override obsolete.
/// </summary>
using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Exports;

public class ExportFormatOverride : BaseEntity
{
    [MaxLength(64)]
    public string FormatKey { get; set; } = string.Empty;

    public string PatchJson { get; set; } = "{}";

    public bool IsEnabled { get; set; } = true;

    [MaxLength(512)]
    public string? Note { get; set; }

    [MaxLength(32)]
    public string CreatedUnderVersion { get; set; } = string.Empty;
}
