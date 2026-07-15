// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Models.Scheduling;

/// <summary>
/// Result of IOvertimeSurchargeCalculator.CalculateAsync: the typed overtime surcharge portions for a
/// single Work (empty when overtime is not configured for the installation or none of the work's hours
/// fall above a configured tier threshold). How these portions combine with the macro's own
/// Night/Weekend/Holiday result (K4) is NOT part of this result — the caller derives the stacking mode
/// from the MacroFunction of the macro that produced the shift's surcharges (Standard = highest wins,
/// StandardAdditive = additive).
/// </summary>
/// <param name="Items">Overtime1/2/3 surcharge portions attributable to this Work, empty when none apply</param>
/// <param name="IsConfigured">
/// False only when the installation has no overtime tiers configured at all (the None() case) — the
/// caller uses this to skip the successor reprocessing cascade entirely for installations that never
/// touch overtime. True whenever tiers exist, even if this particular Work's own Items came back empty
/// (its hours simply did not reach a tier), because a sibling Work in the same basis period can still
/// need its tier band recomputed.
/// </param>
public sealed record OvertimeCalculationResult(
    IReadOnlyList<MacroSurchargeItem> Items,
    bool IsConfigured = true)
{
    public static OvertimeCalculationResult None() =>
        new(Array.Empty<MacroSurchargeItem>(), IsConfigured: false);
}
