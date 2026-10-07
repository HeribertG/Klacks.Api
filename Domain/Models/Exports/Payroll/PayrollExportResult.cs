// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Exports.Payroll;

/// <summary>
/// Output of a payroll formatter. RecordCount is the formatter's own line/row count (per entry, or per employee in
/// matrix layouts). EmittedEntryCount plus all skip counters equals the number of input entries.
/// </summary>
public class PayrollExportResult
{
    public byte[] Content { get; set; } = [];

    public int RecordCount { get; set; }

    public int EmittedEntryCount { get; set; }

    public int SkippedAbsenceCount { get; set; }

    public int SkippedUnsupportedUnitCount { get; set; }

    public int SkippedUnsupportedKindCount { get; set; }

    public int SkippedUnmappedSurchargeCount { get; set; }

    public int SkippedUnmappedBaseWageCount { get; set; }

    public int SkippedSupersededCount { get; set; }

    public bool AbsenceMappingInvalid { get; set; }

    public int TotalSkippedCount =>
        SkippedAbsenceCount
        + SkippedUnsupportedUnitCount
        + SkippedUnsupportedKindCount
        + SkippedUnmappedSurchargeCount
        + SkippedUnmappedBaseWageCount
        + SkippedSupersededCount;
}