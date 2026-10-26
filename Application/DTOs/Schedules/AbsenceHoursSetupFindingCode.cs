// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The deterministic findings the absence-hours setup advisor can raise about an employee's master data.
/// </summary>
public enum AbsenceHoursSetupFindingCode
{
    NoContract = 0,
    ContractIgnoredByPaymentInterval = 1,
    ZeroGuaranteedHoursByDesign = 2,
    PercentFieldIgnoredOnExplicitPath = 3,
    SchedulingRuleOverridesContractHours = 4,
    SchedulingRuleSetsDailyHours = 5,
    ImplausibleWorkloadBasis = 6,
    NoMacroOnAbsenceType = 7,
    MonthlyRowGivesFullWorkloadWithoutContract = 8,
    StoredEntryDiffersFromCalculation = 9,
    DesiredHoursReached = 10,
    DesiredHoursNeedChange = 11,
    SetupConsistent = 12,
    MonthlyRowsIncomplete = 13,
    MonthlyRowDeviatesFromWorkdays = 14
}
