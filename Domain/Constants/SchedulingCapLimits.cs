// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Valid range of a scheduling cap (maximum daily hours, maximum weekly hours, maximum consecutive days) on a
/// scheduling rule, in the company-wide settings and in a planning profile: empty inherits the next level, a value
/// must be at least <see cref="MinValue"/>. A cap of 0 is rejected on input in scheduling rules, company settings and
/// planning profiles (owner rule 2026-11-07); minimums such as the rest hours or rest days are not caps, there 0 means
/// no requirement.
/// </summary>
/// <param name="value">Cap value; null means inherit</param>

namespace Klacks.Api.Domain.Constants;

public static class SchedulingCapLimits
{
    public const decimal MinValue = 1m;

    public static bool IsValid(decimal? value) => value is null or >= MinValue;
}
