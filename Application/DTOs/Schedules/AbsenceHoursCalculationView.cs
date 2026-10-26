// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The absence-hours calculation as Klacksy relays it: every figure next to the place it was taken from, the
/// formula the macro applied and the result. Flat and language-neutral; sources are enum names the model wording
/// translates.
/// </summary>
public sealed record AbsenceHoursCalculationView
{
    public required string Employee { get; init; }
    public required string Date { get; init; }
    public required string AbsenceType { get; init; }
    public string? Macro { get; init; }
    public required string PaymentIntervalUsed { get; init; }

    public required bool HasActiveContract { get; init; }
    public string? ContractUsed { get; init; }
    public required IReadOnlyList<string> ContractsIgnoredByPaymentInterval { get; init; }
    public string? SchedulingRule { get; init; }

    public required decimal DailyHours { get; init; }
    public required string DailyHoursSource { get; init; }
    public required decimal WorkloadPercent { get; init; }
    public required string WorkloadPercentSource { get; init; }
    public string? WorkloadPercentFormula { get; init; }
    public required decimal GuaranteedHours { get; init; }
    public required string GuaranteedHoursSource { get; init; }
    public required bool GuaranteedHoursScaledByContractPercent { get; init; }
    public required decimal FullTime { get; init; }
    public decimal? MonthlyTargetHoursRow { get; init; }

    public required int IsoWeekday { get; init; }
    public required bool IsWeekendDay { get; init; }
    public required bool IsHoliday { get; init; }

    public required bool MacroExecuted { get; init; }
    public decimal? ResultHours { get; init; }
    public required string ResultFormula { get; init; }

    public decimal? StoredHoursOfExistingEntry { get; init; }
    public bool? ExistingEntryHasDirectlyRecordedDuration { get; init; }
    public string? ExistingEntryLockLevel { get; init; }

    public required AbsenceHoursSettingsSummary CompanySettings { get; init; }
}
