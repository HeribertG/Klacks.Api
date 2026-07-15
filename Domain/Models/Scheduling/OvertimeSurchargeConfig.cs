// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

/// <summary>
/// Resolved overtime surcharge configuration for a single client/date, built from the
/// SchedulingRule/Contract/Settings OvertimeThreshold fallback chain (tier 1's AfterHours only) plus
/// the OVERTIME_* / SURCHARGE_STACKING_MODE settings written by region-setup.json (K3/K4). An empty
/// Tiers list means overtime surcharges are not configured for this installation — callers must treat
/// that as a no-op, not as "zero hours of overtime".
/// </summary>
/// <remarks>
/// RateMode is restricted to Multiplier/FixedPerHour (FixedPerShift is rejected at region-setup import
/// time — a flat per-shift amount cannot be split across tiers by hours worked). Per the same arithmetic
/// invariant K19 established for the macro-level rate modes, Multiplier and FixedPerHour compute the tier
/// amount identically (TierHours * Rate — bonus hours vs. an absolute currency amount, same formula,
/// different unit); RateMode currently has no effect on OvertimeSurchargeCalculator's arithmetic and is
/// carried here only for validation/documentation and forward-compatibility (e.g. a future minimum-per-hour
/// floor).
/// </remarks>
public sealed class OvertimeSurchargeConfig
{
    public OvertimeBasis Basis { get; init; } = OvertimeBasis.Day;

    public SurchargeRateMode RateMode { get; init; } = SurchargeRateMode.Multiplier;

    public SurchargeStackingMode StackingMode { get; init; } = SurchargeStackingMode.HighestWins;

    public IReadOnlyList<OvertimeTierConfig> Tiers { get; init; } = Array.Empty<OvertimeTierConfig>();
}
