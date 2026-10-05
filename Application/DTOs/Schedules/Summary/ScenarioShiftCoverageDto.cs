// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.Summary;

/// <summary>
/// Coverage of one shift inside a scenario for the scenario's period.
/// </summary>
/// <param name="ShiftId">Id of the scenario's shift.</param>
/// <param name="ShiftName">Display name of the shift.</param>
/// <param name="Abbreviation">Short name of the shift.</param>
/// <param name="DemandedSlots">Employee slots the shift needs in the period.</param>
/// <param name="FilledSlots">Slots covered by a work of the scenario.</param>
public sealed record ScenarioShiftCoverageDto(
    Guid ShiftId,
    string ShiftName,
    string Abbreviation,
    int DemandedSlots,
    int FilledSlots)
{
    public int OpenSlots => DemandedSlots - FilledSlots;
}
