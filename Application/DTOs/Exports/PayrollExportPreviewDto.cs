// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a payroll export of a period would do right now, without writing anything.
/// @param CanExport - True when the period is complete and at least one person is new or changed
/// @param IsComplete - True when the completeness gate found no blocker
/// @param PersonCount - Persons with closed payroll entries in scope (new, changed and unchanged)
/// @param NewOrChangedPersons - Persons the export would contain
/// @param AlreadyExportedCount - Persons in scope whose content was already exported unchanged
/// @param Blockers - The blockers of the completeness gate, capped
/// @param BlockerTotal - Number of blockers found before the cap
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class PayrollExportPreviewDto
{
    public bool CanExport { get; set; }

    public bool IsComplete { get; set; }

    public int PersonCount { get; set; }

    public List<PayrollExportPersonDto> NewOrChangedPersons { get; set; } = [];

    public int AlreadyExportedCount { get; set; }

    public List<PayrollExportBlockerDto> Blockers { get; set; } = [];

    public int BlockerTotal { get; set; }
}
