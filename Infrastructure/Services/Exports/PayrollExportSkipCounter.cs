// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Counts, per payroll export, every entry a formatter did not write and why, so that no entry is dropped
/// silently: an unmapped absence, a quantity unit the target layout cannot carry (days into an hours field),
/// an entry kind the formatter does not know, a surcharge or worked-hours entry without a configured wage type,
/// and an entry superseded by another entry of the same day in a one-cell-per-day layout. It also records an
/// absence mapping that could not be parsed. Every formatter must account for each input entry exactly once:
/// either in Emitted or in one of the skip counters.
/// </summary>

using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Infrastructure.Services.Exports;

public sealed class PayrollExportSkipCounter
{
    public int Emitted { get; set; }

    public int UnmappedAbsences { get; set; }

    public int UnsupportedUnits { get; set; }

    public int UnsupportedKinds { get; set; }

    public int UnmappedSurcharges { get; set; }

    public int UnmappedBaseWages { get; set; }

    public int Superseded { get; set; }

    public bool AbsenceMappingInvalid { get; set; }

    public PayrollExportResult ToResult(byte[] content, int recordCount)
    {
        return new PayrollExportResult
        {
            Content = content,
            RecordCount = recordCount,
            EmittedEntryCount = Emitted,
            SkippedAbsenceCount = UnmappedAbsences,
            SkippedUnsupportedUnitCount = UnsupportedUnits,
            SkippedUnsupportedKindCount = UnsupportedKinds,
            SkippedUnmappedSurchargeCount = UnmappedSurcharges,
            SkippedUnmappedBaseWageCount = UnmappedBaseWages,
            SkippedSupersededCount = Superseded,
            AbsenceMappingInvalid = AbsenceMappingInvalid,
        };
    }
}