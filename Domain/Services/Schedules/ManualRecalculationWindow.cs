// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Caps the pay periods of a manual period-hours recalculation (normal and thorough): a caller without the admin role
/// recalculates at most PayPeriodLimits.MaxPeriodsForPlannerRecalculation pay periods per request; the admin is bounded
/// only by PayPeriodLimits.MaxPeriodsInRange (GetPeriodsInRangeAsync). The admin role is asked only when the window is
/// over the cap. Refuses with InvalidRequestException (400) before anything is written or queued.
/// </summary>
/// <param name="payPeriodCount">Number of real pay periods the requested window touches</param>
/// <param name="userService">Resolves whether the caller is an admin</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Domain.Services.Schedules;

public static class ManualRecalculationWindow
{
    public static async Task EnsureWithinCallerLimitAsync(int payPeriodCount, IUserService userService)
    {
        if (payPeriodCount <= PayPeriodLimits.MaxPeriodsForPlannerRecalculation || await userService.IsAdmin())
        {
            return;
        }

        throw new InvalidRequestException(
            $"Without the admin role a manual recalculation covers at most {PayPeriodLimits.MaxPeriodsForPlannerRecalculation} " +
            $"pay periods per request; the requested window spans {payPeriodCount}.");
    }
}
