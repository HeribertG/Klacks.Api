// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The deterministic findings the target-hours setup advisor can raise.
/// </summary>
public enum TargetHoursSetupFindingCode
{
    WayInUse = 0,
    MonthlyRowsIncomplete = 1,
    MonthlyRowDeviatesFromWorkdays = 2,
    PercentWithoutEffect = 3,
    ContradictoryContractFields = 4,
    ContractFilteredByViewInterval = 5,
    NoContractButNotFullWorkload = 6,
    ImplausibleWorkloadBasis = 7,
    SetupConsistent = 8
}
