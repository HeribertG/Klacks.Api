// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupingEligibilityOracle
{
    EligibilityVerdict Evaluate(Guid clientId, Guid shiftId);

    bool IsEligible(Guid clientId, Guid shiftId);

    IReadOnlySet<DayOfWeek> EligibleWeekdays(Guid clientId, Guid shiftId);

    bool HasActiveContractInPeriod(Guid clientId);
}
