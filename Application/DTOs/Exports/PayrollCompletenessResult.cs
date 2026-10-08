// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of the payroll completeness gate for a period.
/// @param Blockers - The blockers, ordered by person name, day and reason, capped at PayrollExportConstants.MaxReportedBlockers
/// @param BlockerTotal - Number of blockers found before the cap
/// @param IsComplete - True when no blocker exists and the export may run
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class PayrollCompletenessResult
{
    public List<PayrollExportBlockerDto> Blockers { get; set; } = [];

    public int BlockerTotal { get; set; }

    public bool IsComplete => BlockerTotal == 0;
}
