// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Why a person (or one of their days) blocks a payroll export in the completeness gate. The numeric order is the
/// order in which blockers of the same person and day are listed.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum PayrollExportBlockReason
{
    /// <summary>A Work or Break of the person on that day has a lock level below Closed.</summary>
    EntryNotClosed = 0,

    /// <summary>The day is not locked by a period-close seal (Level Closed) for the person.</summary>
    DayNotLocked = 1,

    /// <summary>The person was already exported for a different period that overlaps the requested one.</summary>
    OverlappingExport = 2
}
