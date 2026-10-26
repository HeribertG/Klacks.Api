// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one definition of the workload share absence macros scale by: guaranteed hours / full time x 100,
/// guarded against implausible master data. Shared by the contract path and the no-contract path of the
/// contract data provider so both scale a vacation day by the same rule.
/// </summary>
/// <param name="guaranteedHours">Guaranteed hours of the basis (contract, rule or settings)</param>
/// <param name="fullTime">Full-time hours of the basis (rule, contract or settings)</param>
/// <param name="implausibilityReason">Set when the basis was rejected and full workload is returned instead</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Associations;

public static class WorkloadPercentCalculator
{
    // Plausibility ceiling for the workload share derived from GuaranteedHours / FullTime. Modest
    // over-full-time contracts (up to 150 percent) are legitimate and pass through; anything above is
    // treated as broken master data (observed live: a monthly GuaranteedHours of 172.2 against a
    // WEEKLY full-time of 45 would compute 383 percent and credit ~32 hours per vacation day) and
    // falls back to full workload instead of scaling absurdly up.
    public const decimal MaximumPlausiblePercent = 150m;

    public const string NoPositiveFullTimeReason = "no positive full-time basis";
    public const string CeilingExceededReason = "ratio exceeds the plausibility ceiling";

    public static decimal Resolve(decimal guaranteedHours, decimal fullTime, out string? implausibilityReason)
    {
        if (fullTime <= 0)
        {
            implausibilityReason = NoPositiveFullTimeReason;
            return MonthlyTargetHoursConstants.FullWorkloadPercent;
        }

        var percent = guaranteedHours / fullTime * MonthlyTargetHoursConstants.FullWorkloadPercent;
        if (percent > MaximumPlausiblePercent)
        {
            implausibilityReason = CeilingExceededReason;
            return MonthlyTargetHoursConstants.FullWorkloadPercent;
        }

        implausibilityReason = null;
        return percent;
    }
}
