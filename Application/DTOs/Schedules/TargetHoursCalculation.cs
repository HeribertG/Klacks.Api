// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The real target-hours resolution of one employee for one pay period, laid open: the effective contract data with
/// the provenance of every figure, the way it was determined, the contracts of the period start (the winner and the
/// ones a view interval would filter), the monthly table of the year, the workdays per month, the company settings and
/// the prorated period target exactly as the schedule row header and get_period_hours show it.
/// </summary>
public sealed record TargetHoursCalculation
{
    public required Guid ClientId { get; init; }
    public required DateOnly PeriodStart { get; init; }
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Payment interval of the view the facts were flagged against; null when none was given or configured.</summary>
    public int? ViewInterval { get; init; }

    public required EffectiveContractData ContractData { get; init; }
    public required TargetHoursWay Way { get; init; }
    public required IReadOnlyList<AbsenceHoursContractSummary> ContractsActiveAtPeriodStart { get; init; }
    public AbsenceHoursSchedulingRuleSummary? SchedulingRule { get; init; }
    public required AbsenceHoursSettingsSummary Settings { get; init; }

    public decimal? MonthlyTargetHoursRow { get; init; }
    public required IReadOnlyDictionary<int, decimal> MonthlyTargetHoursOfYear { get; init; }
    public required IReadOnlyDictionary<int, int> WorkdaysPerMonth { get; init; }

    /// <summary>The period target as the schedule shows it: effective guaranteed hours prorated by the membership window.</summary>
    public required decimal PeriodTargetHours { get; init; }

    public bool ProratedByMembership => PeriodTargetHours != ContractData.GuaranteedHours;

    public bool ContractIgnoredByPaymentInterval => ContractsActiveAtPeriodStart.Any(contract => contract.FilteredByPaymentInterval);
}
