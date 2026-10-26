// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The real absence-hours calculation of one employee on one day, laid open: the effective contract data with its
/// provenance, the contracts and rule behind it, the company settings, the monthly target hours row, the exact
/// macro input and the macro result. Produced by the production pipeline on a transient entry; nothing is stored.
/// </summary>
public sealed record AbsenceHoursCalculation
{
    public required Guid ClientId { get; init; }
    public required DateOnly Date { get; init; }
    public required Guid AbsenceId { get; init; }
    public required MultiLanguage AbsenceName { get; init; }
    public Guid? MacroId { get; init; }
    public string? MacroName { get; init; }

    /// <summary>Payment interval the calculation filtered contracts with, as the Ui does when it books an absence.</summary>
    public int? PaymentIntervalUsed { get; init; }

    public required EffectiveContractData ContractData { get; init; }
    public required IReadOnlyList<AbsenceHoursContractSummary> ContractsActiveOnDate { get; init; }
    public AbsenceHoursSchedulingRuleSummary? SchedulingRule { get; init; }
    public required AbsenceHoursSettingsSummary Settings { get; init; }
    public decimal? MonthlyTargetHoursRow { get; init; }

    /// <summary>
    /// Monthly target hours rows of the calculation year (month -> hours) and the workdays of every month of that
    /// year (configured weekend days and official holidays of the applicable calendar excluded). Filled only when
    /// the employee reads the monthly table: without a contract, or with a contract on the MonthlyTargetHours
    /// interval or inheriting the company value. Null otherwise.
    /// </summary>
    public IReadOnlyDictionary<int, decimal>? MonthlyTargetHoursOfYear { get; init; }
    public IReadOnlyDictionary<int, int>? WorkdaysPerMonth { get; init; }

    public required MacroData MacroInput { get; init; }
    public required bool IsWeekendDay { get; init; }

    /// <summary>Null when the absence type has no macro or the macro did not run successfully.</summary>
    public decimal? ResultHours { get; init; }
    public required bool MacroExecuted { get; init; }

    public AbsenceHoursExistingEntry? ExistingEntry { get; init; }

    /// <summary>True when at least one active contract of the day was filtered out by the payment interval.</summary>
    public bool ContractIgnoredByPaymentInterval => ContractsActiveOnDate.Any(contract => contract.FilteredByPaymentInterval);
}
