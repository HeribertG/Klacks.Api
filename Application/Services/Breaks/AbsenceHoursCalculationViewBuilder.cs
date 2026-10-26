// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Projects an <see cref="AbsenceHoursCalculation"/> into the flat view Klacksy relays, naming every source and
/// writing out the two formulas (workload percent, resulting hours) from the figures the pipeline really used.
/// </summary>
/// <param name="calculation">The calculation as produced by the production pipeline</param>
/// <param name="employeeName">Display name of the employee</param>
/// <param name="language">Language the absence type name is rendered in</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Breaks;

public static class AbsenceHoursCalculationViewBuilder
{
    private const string PercentFormulaTemplate = "{0} / {1} x 100 = {2} %";
    private const string ZeroDayFormula = "weekend day or holiday -> 0 h";
    private const string ZeroGateFormula = "guaranteed hours are 0 -> 0 h";
    private const string ScaledFormula = "{0} h x {1} % = {2} h";
    private const string NoMacroFormula = "the absence type has no macro: the hours stay as booked";
    private const string MacroFailedFormula = "the macro did not run successfully; the booked hours stay unchanged";
    private const string MacroOwnFormula = "macro result {0} h (the macro of this absence type applies its own rule)";
    private const int HoursDecimals = 2;

    public static AbsenceHoursCalculationView Build(AbsenceHoursCalculation calculation, string employeeName, string? language)
    {
        var data = calculation.ContractData;
        var contractUsed = calculation.ContractsActiveOnDate.FirstOrDefault(contract => contract.UsedByCalculation);

        return new AbsenceHoursCalculationView
        {
            Employee = employeeName,
            Date = calculation.Date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            AbsenceType = calculation.AbsenceName.GetValueOrFirstAvailable(language),
            Macro = calculation.MacroName,
            PaymentIntervalUsed = NameInterval(calculation.PaymentIntervalUsed),
            HasActiveContract = data.HasActiveContract,
            ContractUsed = contractUsed?.Name,
            ContractsIgnoredByPaymentInterval = calculation.ContractsActiveOnDate
                .Where(contract => contract.FilteredByPaymentInterval)
                .Select(contract => $"{contract.Name} ({contract.PaymentInterval})")
                .ToList(),
            SchedulingRule = calculation.SchedulingRule?.Name,
            DailyHours = data.DefaultWorkingHours,
            DailyHoursSource = data.DefaultWorkingHoursSource.ToString(),
            WorkloadPercent = Round(data.WorkloadPercent),
            WorkloadPercentSource = data.WorkloadPercentSource.ToString(),
            WorkloadPercentFormula = data.WorkloadRatioBasis is { } basis
                ? string.Format(CultureInfo.InvariantCulture, PercentFormulaTemplate, basis.GuaranteedHours, basis.FullTime, Round(data.WorkloadPercent))
                : null,
            GuaranteedHours = data.GuaranteedHours,
            GuaranteedHoursSource = data.GuaranteedHoursSource.ToString(),
            GuaranteedHoursScaledByContractPercent = data.GuaranteedHoursScaledByContractPercent,
            FullTime = data.FullTime,
            MonthlyTargetHoursRow = calculation.MonthlyTargetHoursRow,
            IsoWeekday = calculation.MacroInput.Weekday,
            IsWeekendDay = calculation.IsWeekendDay,
            IsHoliday = calculation.MacroInput.Holiday,
            MacroExecuted = calculation.MacroExecuted,
            ResultHours = calculation.ResultHours,
            ResultFormula = DescribeResult(calculation),
            StoredHoursOfExistingEntry = calculation.ExistingEntry?.StoredHours,
            ExistingEntryHasDirectlyRecordedDuration = calculation.ExistingEntry?.HasDirectlyRecordedDuration,
            ExistingEntryLockLevel = calculation.ExistingEntry?.LockLevel.ToString(),
            CompanySettings = calculation.Settings
        };
    }

    // Describes the seeded absence macros' rule from the inputs; a macro with its own rule is reported by result only.
    private static string DescribeResult(AbsenceHoursCalculation calculation)
    {
        if (calculation.MacroId == null)
        {
            return NoMacroFormula;
        }

        if (!calculation.MacroExecuted || calculation.ResultHours is not { } result)
        {
            return MacroFailedFormula;
        }

        var input = calculation.MacroInput;
        if (result == decimal.Zero && (calculation.IsWeekendDay || input.Holiday))
        {
            return ZeroDayFormula;
        }

        if (result == decimal.Zero && input.GuaranteedHours <= decimal.Zero)
        {
            return ZeroGateFormula;
        }

        var scaled = Round(input.Hour * input.WorkloadPercent / MonthlyTargetHoursConstants.FullWorkloadPercent);
        return scaled == Round(result)
            ? string.Format(CultureInfo.InvariantCulture, ScaledFormula, input.Hour, Round(input.WorkloadPercent), scaled)
            : string.Format(CultureInfo.InvariantCulture, MacroOwnFormula, Round(result));
    }

    private static string NameInterval(int? interval) =>
        interval.HasValue && Enum.IsDefined(typeof(PaymentInterval), interval.Value)
            ? ((PaymentInterval)interval.Value).ToString()
            : interval?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static decimal Round(decimal value) => Math.Round(value, HoursDecimals, MidpointRounding.AwayFromZero);
}
