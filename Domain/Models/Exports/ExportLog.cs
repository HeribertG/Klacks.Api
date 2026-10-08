// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Domain.Models.Exports;

/// <summary>
/// History entry for every successful export run (order and payroll). A payroll export is person-based and carries
/// no group; its per-person content is recorded in ExportLogItem rows, PersonCount is the number of persons in the
/// file, IsSupplementary marks a re-export of persons that had been exported for the same period before, and
/// StorageKey points at the stored artifact for re-download.
/// Used to warn admins when unsealing a period that has already been exported.
/// </summary>
/// <remarks>
/// ExportedAt and ExportedBy are deliberately kept separate from BaseEntity.CreateTime
/// and CurrentUserCreated: the audit moment is a domain concept that must remain stable
/// even if the row-insert infrastructure later changes its semantics (batching, retries,
/// snapshot restores). Do not remove these fields in favour of the inherited ones.
/// The Skipped* counters and AbsenceMappingInvalid record, for payroll exports, which entries the formatter could
/// not write and why (see PayrollExportResult), so a partial export stays visible in the export history.
/// </remarks>
public class ExportLog : BaseEntity
{
    [MaxLength(ExportLogLimits.FormatMaxLength)]
    public string Format { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Guid? GroupId { get; set; }

    [MaxLength(ExportLogLimits.LanguageMaxLength)]
    public string Language { get; set; } = "de";

    [MaxLength(16)]
    public string CurrencyCode { get; set; } = "EUR";

    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public int RecordCount { get; set; }

    public DateTime ExportedAt { get; set; }

    [MaxLength(256)]
    public string ExportedBy { get; set; } = string.Empty;

    public bool OverrideApplied { get; set; }

    public int SkippedAbsenceCount { get; set; }

    public int SkippedUnsupportedUnitCount { get; set; }

    public int SkippedUnsupportedKindCount { get; set; }

    public int SkippedUnmappedSurchargeCount { get; set; }

    public int SkippedUnmappedBaseWageCount { get; set; }

    public int SkippedSupersededCount { get; set; }

    public bool AbsenceMappingInvalid { get; set; }

    public bool IsSupplementary { get; set; }

    public int PersonCount { get; set; }

    [MaxLength(ExportLogLimits.StorageKeyMaxLength)]
    public string? StorageKey { get; set; }

    /// <summary>Copies the skip counters of a payroll formatter run onto this history entry.</summary>
    /// <param name="result">The formatter result whose counters are recorded</param>
    public void RecordSkips(PayrollExportResult result)
    {
        SkippedAbsenceCount = result.SkippedAbsenceCount;
        SkippedUnsupportedUnitCount = result.SkippedUnsupportedUnitCount;
        SkippedUnsupportedKindCount = result.SkippedUnsupportedKindCount;
        SkippedUnmappedSurchargeCount = result.SkippedUnmappedSurchargeCount;
        SkippedUnmappedBaseWageCount = result.SkippedUnmappedBaseWageCount;
        SkippedSupersededCount = result.SkippedSupersededCount;
        AbsenceMappingInvalid = result.AbsenceMappingInvalid;
    }
}
