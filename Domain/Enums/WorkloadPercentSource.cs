// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// How the workload share (percent) of effective contract data was derived. Absence macros scale a
/// paid absence day by this share; the source tells which master data decides it.
/// </summary>
public enum WorkloadPercentSource
{
    /// <summary>Client without a contract: full workload, like a contract that inherits the company value with an empty percent (owner decision 2026-10-25).</summary>
    NoContractFullWorkload = 0,

    /// <summary>Contract with explicit guaranteed hours: contract guaranteed hours / effective full time.</summary>
    ContractRatio = 1,

    /// <summary>Contract whose scheduling rule sets the guaranteed hours: rule guaranteed hours / effective full time.</summary>
    SchedulingRuleRatio = 2,

    /// <summary>Contract inheriting the company value or on the monthly target hours interval: its Percent field.</summary>
    ContractPercent = 3,

    /// <summary>Same as ContractPercent, but the Percent field is empty and defaults to full workload.</summary>
    ContractPercentDefault = 4,

    /// <summary>Client without a contract in a month that has a monthly target hours row: full workload, nothing to scale by.</summary>
    MonthlyTargetHoursFullWorkload = 5,

    /// <summary>The ratio basis was implausible (no positive full time, or above the plausibility ceiling): full workload fallback.</summary>
    ImplausibleMasterDataFallback = 6
}
