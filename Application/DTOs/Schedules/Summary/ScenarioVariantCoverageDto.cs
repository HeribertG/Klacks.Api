// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.Summary;

/// <summary>
/// Coverage of one variant (key) inside a scenario: the planned shifts cloned from the key's services, summed over the period.
/// </summary>
/// <param name="VariantId">Id of the variant row (analyse_scenarios, Kind = Variant).</param>
/// <param name="Name">Name of the variant.</param>
/// <param name="DemandedSlots">Employee slots the variant's shifts need in the scenario period.</param>
/// <param name="FilledSlots">Slots covered by a work of the scenario.</param>
/// <param name="PeakOpenSlots">Largest number of open slots on one day, summed over all shifts of the variant on that day; 0 when nothing is open.</param>
/// <param name="PeakDate">Earliest day on which <paramref name="PeakOpenSlots"/> is reached; null when nothing is open.</param>
/// <param name="CopiedShiftCount">Orders of this variant copied from a real order that is still live and overlaps the variant's period; accepting the scenario leaves both the copy and that original standing.</param>
public sealed record ScenarioVariantCoverageDto(
    Guid VariantId,
    string Name,
    int DemandedSlots,
    int FilledSlots,
    int PeakOpenSlots,
    DateOnly? PeakDate,
    int CopiedShiftCount)
{
    public int OpenSlots => DemandedSlots - FilledSlots;
}
