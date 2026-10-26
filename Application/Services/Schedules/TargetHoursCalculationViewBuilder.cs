// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Projects a <see cref="TargetHoursCalculation"/> into the flat view Klacksy relays, naming every source and writing
/// out the guaranteed-hours formula from the figures the resolver really used.
/// </summary>
/// <param name="calculation">The resolution as produced by the production pipeline</param>
/// <param name="employeeName">Display name of the employee</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Schedules;

public static class TargetHoursCalculationViewBuilder
{
    private const int Decimals = 2;

    public static TargetHoursCalculationView Build(TargetHoursCalculation calculation, string employeeName)
    {
        var data = calculation.ContractData;
        var contractUsed = calculation.ContractsActiveAtPeriodStart.FirstOrDefault(contract => contract.UsedByCalculation);

        return new TargetHoursCalculationView
        {
            Employee = employeeName,
            PeriodStart = calculation.PeriodStart.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            PeriodEnd = calculation.PeriodEnd.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            Way = calculation.Way.ToString(),
            HasActiveContract = data.HasActiveContract,
            ContractUsed = contractUsed?.Name,
            ContractPaymentInterval = contractUsed?.PaymentInterval.ToString(),
            ContractsFilteredByViewInterval = calculation.ContractsActiveAtPeriodStart
                .Where(contract => contract.FilteredByPaymentInterval)
                .Select(contract => $"{contract.Name} ({contract.PaymentInterval})")
                .ToList(),
            ViewInterval = NameInterval(calculation.ViewInterval),
            SchedulingRule = calculation.SchedulingRule?.Name,
            GuaranteedHours = data.GuaranteedHours,
            GuaranteedHoursSource = data.GuaranteedHoursSource.ToString(),
            GuaranteedHoursFormula = DescribeGuaranteedHours(calculation),
            WorkloadPercent = Round(data.WorkloadPercent),
            WorkloadPercentSource = data.WorkloadPercentSource.ToString(),
            MonthlyTargetHoursRow = calculation.MonthlyTargetHoursRow,
            MonthsWithRowInYear = calculation.MonthlyTargetHoursOfYear.Count,
            FullTime = data.FullTime,
            DailyHours = data.DefaultWorkingHours,
            WorkdaysInMonth = calculation.WorkdaysPerMonth.GetValueOrDefault(calculation.PeriodStart.Month),
            PeriodTargetHours = calculation.PeriodTargetHours,
            ProratedByMembership = calculation.ProratedByMembership,
            CompanySettings = calculation.Settings
        };
    }

    private static string DescribeGuaranteedHours(TargetHoursCalculation calculation)
    {
        var data = calculation.ContractData;
        var basis = data.GuaranteedHoursSource switch
        {
            ContractValueSource.MonthlyTargetHours => $"monthly row {calculation.MonthlyTargetHoursRow} h",
            ContractValueSource.Settings => $"company guaranteed hours {calculation.Settings.GuaranteedHours} h",
            ContractValueSource.SchedulingRule => $"scheduling rule guaranteed hours {Round(data.GuaranteedHours)} h",
            _ => $"contract guaranteed hours {Round(data.GuaranteedHours)} h"
        };

        var scaled = data.GuaranteedHoursScaledByContractPercent
            ? $" x {Round(data.WorkloadPercent)} % = {Round(data.GuaranteedHours)} h"
            : string.Empty;
        var prorated = calculation.ProratedByMembership
            ? $"; prorated by membership to {Round(calculation.PeriodTargetHours)} h for the period"
            : string.Empty;

        return basis + scaled + prorated;
    }

    private static string? NameInterval(int? interval) =>
        interval.HasValue && Enum.IsDefined(typeof(PaymentInterval), interval.Value)
            ? ((PaymentInterval)interval.Value).ToString()
            : null;

    private static decimal Round(decimal value) => Math.Round(value, Decimals, MidpointRounding.AwayFromZero);
}
