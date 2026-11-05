// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupingEligibilityOracle
{
    EligibilityVerdict Evaluate(Guid clientId, Guid shiftId);

    bool IsEligible(Guid clientId, Guid shiftId);

    IReadOnlySet<DayOfWeek> EligibleWeekdays(Guid clientId, Guid shiftId);

    /// <summary>
    /// True when the client has at least one day with an active contract in the period. Informational only:
    /// a client without one is evaluated with the settings defaults.
    /// </summary>
    bool HasActiveContractInPeriod(Guid clientId);
}
