// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deterministic advice on how an employee's target hours are set up, derived only from the real resolution: which of
/// the four ways is in use and which one fits, gaps and odd values in the monthly table, a percent field without
/// effect, contradictory contract fields, a contract a schedule view would not see, and an employee without a contract
/// who should not be at 100 %. Pure and static, so every rule is pinned by a unit test; the model only relays it.
/// </summary>
/// <param name="calculation">The real resolution with provenance; null in company mode</param>
/// <param name="monthlyRows">Monthly table of the year</param>
/// <param name="workdays">Workdays per month of the year</param>
/// <param name="settings">Company work settings</param>
/// <param name="desiredWorkloadPercent">Workload share the administrator wants, when given</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public static class TargetHoursSetupAdvisor
{
    public const string ContractPlace = "contract (settings page Contracts: payment interval, guaranteed hours, full time, workload percent) - a contract is a template shared by every employee assigned to it";
    public const string AssignmentPlace = "contracts card of the employee (assign an existing contract, or create a fitting one from a template first)";
    public const string MonthlyTargetHoursPlace = "monthly target hours table (settings, Work & Scheduling)";
    public const string WorkSettingsPlace = "company work settings (guaranteed hours, full time, daily working hours, payment interval)";
    public const string SchedulingRulePlace = "scheduling rule attached to the contract";

    private const int Decimals = 2;
    private const decimal PercentTolerance = 0.01m;

    private static readonly HashSet<TargetHoursSetupFindingCode> InformationalCodes =
    [
        TargetHoursSetupFindingCode.WayInUse,
        TargetHoursSetupFindingCode.SetupConsistent
    ];

    public static IReadOnlyList<TargetHoursSetupFinding> Analyse(
        TargetHoursCalculation? calculation,
        IReadOnlyDictionary<int, decimal> monthlyRows,
        IReadOnlyDictionary<int, int> workdays,
        AbsenceHoursSettingsSummary settings,
        decimal? desiredWorkloadPercent)
    {
        var findings = new List<TargetHoursSetupFinding>();

        if (calculation != null)
        {
            findings.Add(WayFinding(calculation));
            AddEmployeeFindings(calculation, desiredWorkloadPercent, findings);
        }

        var readsTable = calculation == null || EmployeeContractFactsLoader.ReadsMonthlyTable(calculation.ContractData);
        if (readsTable)
        {
            AddTableFindings(monthlyRows, workdays, calculation?.ContractData.DefaultWorkingHours ?? settings.DefaultWorkingHours, findings);
        }

        if (findings.All(finding => InformationalCodes.Contains(finding.Code)))
        {
            findings.Add(new TargetHoursSetupFinding(
                TargetHoursSetupFindingCode.SetupConsistent,
                calculation == null
                    ? "The company-wide target hours setup is consistent."
                    : $"The target hours setup is consistent: the period target is {Round(calculation.PeriodTargetHours)} h.",
                null));
        }

        return findings
            .OrderBy(finding => InformationalCodes.Contains(finding.Code) ? 1 : 0)
            .ToList();
    }

    public static string BuildRecommendation(IReadOnlyList<TargetHoursSetupFinding> findings, TargetHoursCalculation? calculation)
    {
        var intro = calculation == null
            ? "Company-wide view of the monthly target hours table."
            : $"Target hours {calculation.PeriodStart.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)} to " +
              $"{calculation.PeriodEnd.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}: {Round(calculation.PeriodTargetHours)} h ({DescribeWay(calculation.Way)}).";

        var leading = findings[0];
        if (InformationalCodes.Contains(leading.Code))
        {
            return $"{intro} Nothing needs to change.";
        }

        var where = leading.WhereToChange == null ? string.Empty : $" Change it in the {leading.WhereToChange}.";
        var more = findings.Count(finding => !InformationalCodes.Contains(finding.Code)) - 1;
        var moreText = more > 0 ? $" {more} further finding(s) are listed." : string.Empty;
        return $"{intro} {leading.Statement}{where}{moreText} Nothing has been changed; the administrator decides, and the system asks for one confirmation before a contract, the table or an assignment is changed.";
    }

    // The decision guide: same workload all year -> fixed hours; company month values vary -> Weg 1 or Weg 2 with a
    // percent; no contract only for 100 % staff.
    private static TargetHoursSetupFinding WayFinding(TargetHoursCalculation calculation)
    {
        var data = calculation.ContractData;
        var fits = calculation.Way switch
        {
            TargetHoursWay.NoContract => "fits only for a 100 % employee; any other share needs a contract",
            TargetHoursWay.FixedHours => "fits when the workload is the same all year; if the company month values should apply, use Weg 1 or Weg 2 with a percent",
            TargetHoursWay.MonthlyTableInterval => "Weg 1: month row x percent, the contract's own hours in months without a row",
            _ => "Weg 2: (month row, otherwise company guaranteed hours) x percent"
        };

        return new TargetHoursSetupFinding(
            TargetHoursSetupFindingCode.WayInUse,
            $"Way in use: {DescribeWay(calculation.Way)} - {fits}. Guaranteed hours {Round(data.GuaranteedHours)} ({data.GuaranteedHoursSource}" +
            $"{(data.GuaranteedHoursScaledByContractPercent ? $" x {Round(data.WorkloadPercent)} %" : string.Empty)}), period target {Round(calculation.PeriodTargetHours)} h" +
            $"{(calculation.ProratedByMembership ? " after membership proration" : string.Empty)}.",
            null);
    }

    private static void AddEmployeeFindings(TargetHoursCalculation calculation, decimal? desiredWorkloadPercent, List<TargetHoursSetupFinding> findings)
    {
        var data = calculation.ContractData;
        var contract = calculation.ContractsActiveAtPeriodStart.FirstOrDefault(candidate => candidate.UsedByCalculation);

        if (calculation.ContractIgnoredByPaymentInterval)
        {
            var filtered = string.Join(", ", calculation.ContractsActiveAtPeriodStart
                .Where(candidate => candidate.FilteredByPaymentInterval)
                .Select(candidate => $"'{candidate.Name}' ({candidate.PaymentInterval})"));
            findings.Add(new TargetHoursSetupFinding(
                TargetHoursSetupFindingCode.ContractFilteredByViewInterval,
                $"The contract {filtered} is not seen by a schedule view on interval {NameInterval(calculation.ViewInterval)}: absences booked there " +
                "treat the employee as having no contract (100 % of the company value), while the period target still reads the contract. " +
                "Move the contract or the group to the same interval (Monthly and Monthly (target hours table) count as one).",
                ContractPlace));
        }

        if (contract != null && calculation.Way == TargetHoursWay.FixedHours && contract.Percent is { } percent)
        {
            findings.Add(new TargetHoursSetupFinding(
                TargetHoursSetupFindingCode.PercentWithoutEffect,
                $"Contract '{contract.Name}' has fixed guaranteed hours ({Describe(contract.GuaranteedHours)}) and a workload percent of {percent}; the percent has no effect on " +
                "the target (it scales only a month row or the inherited company value). Clear the guaranteed hours to use the percent (Weg 2), or clear the percent.",
                ContractPlace));
        }

        if (contract != null && calculation.Way == TargetHoursWay.MonthlyTableInterval && contract.GuaranteedHours is { } fixedHours && fixedHours > decimal.Zero)
        {
            findings.Add(new TargetHoursSetupFinding(
                TargetHoursSetupFindingCode.ContradictoryContractFields,
                $"Contract '{contract.Name}' is on the interval Monthly (target hours table) but also states {fixedHours} guaranteed hours; they apply only in months " +
                "without a row in the table. Decide: Weg 1 with a percent (clear the hours) or a fixed contract (change the interval).",
                ContractPlace));
        }

        if (contract != null && calculation.Way == TargetHoursWay.FixedHours && contract.FullTime is { } fullTime && fullTime <= decimal.Zero
            && data.WorkloadPercentSource != WorkloadPercentSource.ImplausibleMasterDataFallback)
        {
            findings.Add(new TargetHoursSetupFinding(
                TargetHoursSetupFindingCode.ContradictoryContractFields,
                $"Contract '{contract.Name}' has no full time of its own; the company full time ({calculation.Settings.FullTime}) is used to derive the workload share " +
                $"({Round(data.WorkloadPercent)} %). Set the contract's full time if that share is not intended.",
                ContractPlace));
        }

        if (data.WorkloadPercentSource == WorkloadPercentSource.ImplausibleMasterDataFallback)
        {
            findings.Add(new TargetHoursSetupFinding(
                TargetHoursSetupFindingCode.ImplausibleWorkloadBasis,
                $"The workload basis of the contract is implausible (no positive full time, or a share above {WorkloadPercentCalculator.MaximumPlausiblePercent} %); " +
                "full workload is used as a fallback for absence days.",
                ContractPlace));
        }

        if (calculation.Way == TargetHoursWay.NoContract && desiredWorkloadPercent is { } desired
            && Math.Abs(desired - MonthlyTargetHoursConstants.FullWorkloadPercent) > PercentTolerance)
        {
            findings.Add(new TargetHoursSetupFinding(
                TargetHoursSetupFindingCode.NoContractButNotFullWorkload,
                $"The employee has no contract and therefore counts as 100 %, but {Round(desired)} % is wanted. Without a contract no other share is possible: assign a " +
                $"contract on Weg 1 or Weg 2 with workload percent {Round(desired)}, or a fixed contract with {Round(calculation.Settings.FullTime * desired / MonthlyTargetHoursConstants.FullWorkloadPercent)} " +
                $"guaranteed hours at a full time of {calculation.Settings.FullTime}.",
                AssignmentPlace));
        }
    }

    private static void AddTableFindings(
        IReadOnlyDictionary<int, decimal> rows, IReadOnlyDictionary<int, int> workdays, decimal dailyHours, List<TargetHoursSetupFinding> findings)
    {
        if (MonthlyTableAnalyzer.IsPartiallyFilled(rows))
        {
            findings.Add(new TargetHoursSetupFinding(
                TargetHoursSetupFindingCode.MonthlyRowsIncomplete,
                $"The monthly target hours table has rows for {rows.Count} of 12 months (missing: {string.Join(", ", MonthlyTableAnalyzer.MissingMonths(rows))}). " +
                "In those months the target falls back to the contract or the settings value, so the basis switches during the year. Fill every month, or use fixed contracts.",
                MonthlyTargetHoursPlace));
        }

        var deviating = MonthlyTableAnalyzer.DeviatingMonths(rows, workdays, dailyHours);
        if (deviating.Count > 0)
        {
            findings.Add(new TargetHoursSetupFinding(
                TargetHoursSetupFindingCode.MonthlyRowDeviatesFromWorkdays,
                $"Monthly values deviate by more than {MonthlyTableAnalyzer.DeviationThresholdPercent} % from workdays x daily hours: {string.Join("; ", deviating)}. " +
                "A full month of vacation would then not equal the month's target. Correct the month values, or use fixed contracts.",
                MonthlyTargetHoursPlace));
        }
    }

    public static string DescribeWay(TargetHoursWay way) => way switch
    {
        TargetHoursWay.NoContract => "no contract, company value at 100 %",
        TargetHoursWay.FixedHours => "fixed contract hours",
        TargetHoursWay.MonthlyTableInterval => "Weg 1, interval Monthly (target hours table) with percent",
        _ => "Weg 2, inherited company value with percent"
    };

    private static string Describe(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "inherited";

    private static string NameInterval(int? interval) =>
        interval.HasValue && Enum.IsDefined(typeof(PaymentInterval), interval.Value)
            ? ((PaymentInterval)interval.Value).ToString()
            : "none";

    // Normalised so a whole number prints as "75", not "75.00".
    private static decimal Round(decimal value) =>
        Math.Round(value, Decimals, MidpointRounding.AwayFromZero) / NormalisingOne;

    private const decimal NormalisingOne = 1.000000000000000000000000000m;
}
