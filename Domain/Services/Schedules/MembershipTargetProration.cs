// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Scales the planning target (guaranteed hours, full-time and minimum hours an engine aims for) of an agent whose
/// company membership starts or ends inside the target period: target x (calendar days of membership within the
/// target period) / (calendar days of the target period). The target period is the pay period the target refers to, not the
/// planned range: planning only 25.-31.03. with an exit on 27.03. still gives 27/31, because CurrentHours already carries the
/// hours before the range. An exit on 15.03. in March gives 15/31 of the target, an entry on 10.03.
/// gives 22/31. Klacks only plans; this is the target the engine strives for, not a payroll value. MaximumHours is a
/// hard ceiling and is never scaled: the agent may not work more than the contract allows, but a shorter membership
/// must not make the engine pack the full monthly target into the remaining days. Wizard 2/3 apply the same rule day by
/// day while summing their pay-period target over the planned range (HarmonizerContextBuilder.ComputePeriodTargetHours
/// skips non-member days); its target is range-based by construction. The period hours (PeriodHoursService,
/// WorkRepository.GetPeriodHoursForClients) report the same prorated target, so the schedule's target column,
/// find_replacement, recovery and the target-drift trigger rank against it too.
/// </summary>
public static class MembershipTargetProration
{
    /// <summary>
    /// The share of the target that falls into the membership, or null when nothing is to be scaled (no membership row,
    /// or the membership covers the whole period), so callers keep the unscaled value bit for bit.
    /// </summary>
    /// <param name="window">Membership window of the agent; null when the agent has none (unrestricted)</param>
    /// <param name="from">First day of the target (pay) period</param>
    /// <param name="until">Last day of the target (pay) period</param>
    public static decimal? FactorFor(MembershipWindow? window, DateOnly from, DateOnly until)
    {
        if (window is null || until < from)
        {
            return null;
        }

        var periodDays = until.DayNumber - from.DayNumber + 1;
        var memberDays = window.MemberDaysWithin(from, until);
        return memberDays == periodDays ? null : (decimal)memberDays / periodDays;
    }

    /// <summary>
    /// The pay-period target (GuaranteedHours) of the period hours shown in the schedule and used for replacement and
    /// recovery ranking, prorated by member days and rounded to <see cref="TargetHoursDecimals"/> decimals; unchanged when
    /// <see cref="FactorFor"/> yields null.
    /// </summary>
    /// <param name="target">Unscaled pay-period target</param>
    /// <param name="window">Membership window of the agent; null when the agent has none (unrestricted)</param>
    /// <param name="from">First day of the pay period</param>
    /// <param name="until">Last day of the pay period</param>
    public static decimal ProratedTarget(decimal target, MembershipWindow? window, DateOnly from, DateOnly until)
        => FactorFor(window, from, until) is { } factor
            ? Math.Round(target * factor, TargetHoursDecimals, MidpointRounding.AwayFromZero)
            : target;

    public const int TargetHoursDecimals = 2;
}
