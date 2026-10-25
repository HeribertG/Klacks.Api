// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The target-hours resolution as Klacksy relays it: every figure next to its source, the way, the formula and the
/// prorated period target. Flat and language-neutral; sources are enum names the model wording translates.
/// </summary>
public sealed record TargetHoursCalculationView
{
    public required string Employee { get; init; }
    public required string PeriodStart { get; init; }
    public required string PeriodEnd { get; init; }
    public required string Way { get; init; }

    public required bool HasActiveContract { get; init; }
    public string? ContractUsed { get; init; }
    public string? ContractPaymentInterval { get; init; }
    public required IReadOnlyList<string> ContractsFilteredByViewInterval { get; init; }
    public string? ViewInterval { get; init; }
    public string? SchedulingRule { get; init; }

    public required decimal GuaranteedHours { get; init; }
    public required string GuaranteedHoursSource { get; init; }
    public required string GuaranteedHoursFormula { get; init; }
    public required decimal WorkloadPercent { get; init; }
    public required string WorkloadPercentSource { get; init; }
    public decimal? MonthlyTargetHoursRow { get; init; }
    public required int MonthsWithRowInYear { get; init; }
    public required decimal FullTime { get; init; }
    public required decimal DailyHours { get; init; }
    public required int WorkdaysInMonth { get; init; }

    public required decimal PeriodTargetHours { get; init; }
    public required bool ProratedByMembership { get; init; }

    public required AbsenceHoursSettingsSummary CompanySettings { get; init; }
}
