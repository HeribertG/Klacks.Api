// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validated overtime configuration of a scheduling rule preset or rate revision; all fields are null
/// when the profile entry declares no overtime.
/// </summary>
/// <param name="Basis">Day or week basis of the overtime thresholds</param>
/// <param name="RateMode">Multiplier or fixed amount per hour</param>
/// <param name="Tier1AfterHours">Hours after which tier 1 applies</param>
/// <param name="Tier1Rate">Rate of tier 1</param>
/// <param name="Tier2AfterHours">Hours after which tier 2 applies</param>
/// <param name="Tier2Rate">Rate of tier 2</param>
/// <param name="Tier3AfterHours">Hours after which tier 3 applies</param>
/// <param name="Tier3Rate">Rate of tier 3</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed record PresetOvertimeValues(
    OvertimeBasis? Basis,
    SurchargeRateMode? RateMode,
    decimal? Tier1AfterHours,
    decimal? Tier1Rate,
    decimal? Tier2AfterHours,
    decimal? Tier2Rate,
    decimal? Tier3AfterHours,
    decimal? Tier3Rate);
