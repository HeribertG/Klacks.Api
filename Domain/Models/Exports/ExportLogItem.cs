// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Exports;

/// <summary>
/// Per-person record of what a payroll export contained: one row per person, period and format of an ExportLog run.
/// ContentHash identifies the exact set of day entries that was exported; a later run compares against the
/// person's latest revision to tell which persons are new or changed (a return to older content counts as
/// changed); EntriesJson keeps the canonical snapshot of those entries.
/// </summary>
/// <param name="ExportLogId">The export run this person was part of</param>
/// <param name="ClientId">The exported person</param>
/// <param name="StartDate">Lower bound (inclusive) of the exported period</param>
/// <param name="EndDate">Upper bound (inclusive) of the exported period</param>
/// <param name="Format">Payroll format key the person was exported for</param>
/// <param name="Revision">1-based counter per person, period and format; the highest revision is the person's latest export</param>
/// <param name="ContentHash">SHA-256 (lowercase hex) over the person's exported day entries</param>
/// <param name="EntryCount">Number of day entries exported for the person</param>
/// <param name="EntriesJson">Canonical JSON snapshot of the exported day entries</param>
/// <param name="IsSupplementary">True when the person had already been exported for the same period and format</param>
public class ExportLogItem : BaseEntity
{
    public Guid ExportLogId { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [MaxLength(ExportLogLimits.FormatMaxLength)]
    public string Format { get; set; } = string.Empty;

    public int Revision { get; set; }

    [MaxLength(ExportLogLimits.ContentHashLength)]
    public string ContentHash { get; set; } = string.Empty;

    public int EntryCount { get; set; }

    public string EntriesJson { get; set; } = "[]";

    public bool IsSupplementary { get; set; }
}
