// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deterministic advice on an employee's absence-hours setup, derived only from the real calculation: which master
/// data decides the credited hours of an absence day, what contradicts what, and - when the administrator names the
/// hours per day they want - which field to change where. Pure and static, so every rule is pinned by a unit test;
/// the language model only relays the result. Actionable findings come first, informational ones last, so the
/// recommendation always leads with something to do.
/// </summary>
/// <param name="calculation">The real calculation of the day with the provenance of every figure</param>
/// <param name="desiredHoursPerDay">Hours an absence day should credit, when the administrator named them</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Associations;

namespace Klacks.Api.Application.Services.Breaks;

public static class AbsenceHoursSetupAdvisor
{
    public const string ContractPlace = "contract (settings page Contracts: guaranteed hours, full time, workload percent, payment interval) - a contract is a template shared by every employee assigned to it";
    public const string AssignmentPlace = "contracts card of the employee (assign an existing contract with assign_contract_by_name, or create a fitting one first)";
    public const string SchedulingRulePlace = "scheduling rule attached to the contract (contract settings page: daily working hours, guaranteed hours, full time)";
    public const string WorkSettingsPlace = "company work settings (guaranteed hours, full time, daily working hours, payment interval) - they apply to every employee without a contract";
    public const string AbsenceTypePlace = "absence type (settings, absence card: macro)";
    public const string MonthlyTargetHoursPlace = "monthly target hours table (settings, Work & Scheduling)";
    public const string RecalculationPlace = "schedule toolbar: recalculate hours for the period";

    private const int Decimals = 2;
    private const decimal PercentTolerance = 0.01m;
    private const decimal HoursTolerance = 0.01m;

    private static readonly HashSet<AbsenceHoursSetupFindingCode> InformationalCodes =
    [
        AbsenceHoursSetupFindingCode.SchedulingRuleSetsDailyHours,
        AbsenceHoursSetupFindingCode.DesiredHoursReached,
        AbsenceHoursSetupFindingCode.SetupConsistent
    ];

    public static IReadOnlyList<AbsenceHoursSetupFinding> Analyse(AbsenceHoursCalculation calculation, decimal? desiredHoursPerDay)
    {
        var findings = new List<AbsenceHoursSetupFinding>();
        var data = calculation.ContractData;

        AddDesiredHoursFinding(calculation, desiredHoursPerDay, findings);

        if (calculation.MacroId == null)
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.NoMacroOnAbsenceType,
                $"The absence type '{calculation.AbsenceName.GetValueOrFirstAvailable(null)}' has no macro, so an entry keeps the hours it was booked with (0 from the schedule grid).",
                AbsenceTypePlace));
        }

        if (calculation.ContractIgnoredByPaymentInterval)
        {
            findings.Add(IgnoredContractFinding(calculation));
        }
        else if (!data.HasActiveContract)
        {
            findings.Add(NoContractFinding(calculation));
        }

        if (data.HasActiveContract)
        {
            AddContractFindings(calculation, findings);
        }

        if (data.WorkloadPercentSource == WorkloadPercentSource.ImplausibleMasterDataFallback)
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.ImplausibleWorkloadBasis,
                $"The workload basis is implausible (no positive full time, or a ratio above {WorkloadPercentCalculator.MaximumPlausiblePercent} %), " +
                "so full workload is used as a fallback.",
                data.HasActiveContract ? ContractPlace : WorkSettingsPlace));
        }

        AddExistingEntryFinding(calculation, findings);
        AddMonthlyTableFindings(calculation, findings);

        if (findings.All(finding => InformationalCodes.Contains(finding.Code)))
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.SetupConsistent,
                $"The setup is consistent: an absence day of this kind credits {Round(calculation.ResultHours ?? decimal.Zero)} h.",
                null));
        }

        return findings
            .OrderBy(finding => InformationalCodes.Contains(finding.Code) ? 1 : 0)
            .ToList();
    }

    public static string BuildRecommendation(IReadOnlyList<AbsenceHoursSetupFinding> findings, AbsenceHoursCalculation calculation)
    {
        var result = calculation.ResultHours is { } hours
            ? $"An absence day of this kind on {calculation.Date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)} credits {Round(hours)} h."
            : "The macro did not produce a value for this day.";

        var leading = findings[0];
        if (InformationalCodes.Contains(leading.Code))
        {
            return $"{result} Nothing needs to change.";
        }

        var where = leading.WhereToChange == null ? string.Empty : $" Change it in the {leading.WhereToChange}.";
        var more = findings.Count > 1 ? $" {findings.Count - 1} further finding(s) are listed." : string.Empty;
        return $"{result} {leading.Statement}{where}{more} Nothing has been changed; the administrator decides, and the system asks for one confirmation before a contract is changed or assigned.";
    }

    private static AbsenceHoursSetupFinding IgnoredContractFinding(AbsenceHoursCalculation calculation)
    {
        var ignored = string.Join(", ", calculation.ContractsActiveOnDate
            .Where(contract => !contract.UsedByCalculation)
            .Select(contract => $"'{contract.Name}' ({contract.PaymentInterval})"));
        var basis = calculation.ContractData.HasActiveContract
            ? "another contract decided the hours"
            : "the company work settings decided the hours as if the employee had no contract";

        return new AbsenceHoursSetupFinding(
            AbsenceHoursSetupFindingCode.ContractIgnoredByPaymentInterval,
            $"The active contract {ignored} was ignored because the absence was calculated with payment interval " +
            $"{NameInterval(calculation.PaymentIntervalUsed)}, so {basis}. The schedule books with the interval of the selected group, " +
            "otherwise with the company interval assumed here; if the schedule works in another interval, repeat with that paymentInterval. " +
            "Otherwise move the contract to the interval the schedule works with - do not assign a second contract for this.",
            ContractPlace);
    }

    private static AbsenceHoursSetupFinding NoContractFinding(AbsenceHoursCalculation calculation)
    {
        var data = calculation.ContractData;
        if (data.WorkloadPercentSource == WorkloadPercentSource.MonthlyTargetHoursFullWorkload)
        {
            return new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.MonthlyRowGivesFullWorkloadWithoutContract,
                $"The employee has no contract and the month has a monthly target hours row ({calculation.MonthlyTargetHoursRow} h), " +
                $"so an absence day credits the full {data.DefaultWorkingHours} h; a contract would let a percent apply.",
                AssignmentPlace);
        }

        return new AbsenceHoursSetupFinding(
            AbsenceHoursSetupFindingCode.NoContract,
            $"The employee has no contract on this day, so the company value applies at full workload: {data.DefaultWorkingHours} h per day x 100 % " +
            $"(guaranteed hours {data.GuaranteedHours} from the {(data.GuaranteedHoursSource == ContractValueSource.MonthlyTargetHours ? "monthly target hours row" : "work settings")}). " +
            "Any other workload share needs a contract.",
            AssignmentPlace);
    }

    private static void AddContractFindings(AbsenceHoursCalculation calculation, List<AbsenceHoursSetupFinding> findings)
    {
        var data = calculation.ContractData;
        var contract = calculation.ContractsActiveOnDate.FirstOrDefault(candidate => candidate.UsedByCalculation);
        var rule = calculation.SchedulingRule;

        if (data.GuaranteedHoursSource == ContractValueSource.Contract && data.GuaranteedHours == decimal.Zero && contract != null)
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.ZeroGuaranteedHoursByDesign,
                $"Contract '{contract.Name}' has 0 guaranteed hours, so every paid absence day credits 0 h by design (on-call style contract). " +
                "Give the contract guaranteed hours if absence days should count.",
                ContractPlace));
        }

        if (rule != null && contract != null)
        {
            AddSchedulingRuleFindings(calculation, contract, rule, findings);
        }

        var ratioSource = data.WorkloadPercentSource is WorkloadPercentSource.ContractRatio or WorkloadPercentSource.SchedulingRuleRatio;
        if (contract?.Percent is { } percent && ratioSource && Math.Abs(percent - data.WorkloadPercent) > PercentTolerance)
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.PercentFieldIgnoredOnExplicitPath,
                $"Contract '{contract.Name}' states {percent} % in its workload percent field, but because it has explicit guaranteed hours the " +
                $"ratio guaranteed hours / full time ({Round(data.WorkloadPercent)} %) applies; the percent field only scales an inherited or monthly basis.",
                ContractPlace));
        }
    }

    // The rule's guaranteed hours win only on the explicit path (GuaranteedHoursSource == SchedulingRule); an
    // inheriting contract skips them by design, so no override is reported there. The rule's full time always wins.
    private static void AddSchedulingRuleFindings(
        AbsenceHoursCalculation calculation,
        AbsenceHoursContractSummary contract,
        AbsenceHoursSchedulingRuleSummary rule,
        List<AbsenceHoursSetupFinding> findings)
    {
        var data = calculation.ContractData;
        var overrides = new List<string>();
        if (data.GuaranteedHoursSource == ContractValueSource.SchedulingRule && rule.GuaranteedHours is { } ruleHours && ruleHours != contract.GuaranteedHours)
        {
            overrides.Add($"guaranteed hours {ruleHours} (contract: {Describe(contract.GuaranteedHours)})");
        }

        if (rule.FullTimeHours is { } ruleFullTime && ruleFullTime != contract.FullTime)
        {
            overrides.Add($"full time {ruleFullTime} (contract: {Describe(contract.FullTime)})");
        }

        if (overrides.Count > 0)
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.SchedulingRuleOverridesContractHours,
                $"The scheduling rule '{rule.Name}' overrides the contract: {string.Join(", ", overrides)}. A value the rule sets wins over the contract's own field.",
                SchedulingRulePlace));
        }

        if (rule.DefaultWorkingHours is { } ruleDaily)
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.SchedulingRuleSetsDailyHours,
                $"The daily working hours ({ruleDaily} h) come from the scheduling rule '{rule.Name}', not from the company settings ({calculation.Settings.DefaultWorkingHours} h).",
                SchedulingRulePlace));
        }
    }

    private static void AddExistingEntryFinding(AbsenceHoursCalculation calculation, List<AbsenceHoursSetupFinding> findings)
    {
        if (calculation.ExistingEntry is not { } entry || calculation.ResultHours is not { } result)
        {
            return;
        }

        if (entry.HasDirectlyRecordedDuration || Math.Abs(entry.StoredHours - result) <= HoursTolerance)
        {
            return;
        }

        var recalculation = entry.LockLevel == WorkLockLevel.None
            ? "A recalculation of the period updates it."
            : $"It is sealed ({entry.LockLevel}); reopen the day before a recalculation can touch it.";
        var intervalCaveat = calculation.ContractIgnoredByPaymentInterval
            ? " A recalculation reads the contract regardless of the payment interval, so it may yield the contract's value instead of this one."
            : string.Empty;
        findings.Add(new AbsenceHoursSetupFinding(
            AbsenceHoursSetupFindingCode.StoredEntryDiffersFromCalculation,
            $"The stored entry of this day carries {Round(entry.StoredHours)} h while the current master data yields {Round(result)} h. {recalculation}{intervalCaveat}",
            RecalculationPlace));
    }

    private static void AddDesiredHoursFinding(
        AbsenceHoursCalculation calculation, decimal? desiredHoursPerDay, List<AbsenceHoursSetupFinding> findings)
    {
        if (desiredHoursPerDay is not { } desired)
        {
            return;
        }

        var data = calculation.ContractData;
        var dailyHours = data.DefaultWorkingHours;
        var standardCredit = Round(dailyHours * data.WorkloadPercent / MonthlyTargetHoursConstants.FullWorkloadPercent);
        var weekdayCredit = !calculation.IsWeekendDay && !calculation.MacroInput.Holiday && calculation.ResultHours is { } result
            ? Round(result)
            : standardCredit;

        if (Math.Abs(weekdayCredit - desired) <= HoursTolerance)
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.DesiredHoursReached,
                $"A weekday absence of this kind already credits the desired {Round(desired)} h ({dailyHours} h x {Round(data.WorkloadPercent)} %).",
                null));
            return;
        }

        var ownRule = weekdayCredit != standardCredit
            ? $" The absence type applies its own rule on top (standard rule {standardCredit} h, actual {weekdayCredit} h); the figures assume the standard rule."
            : string.Empty;
        findings.Add(new AbsenceHoursSetupFinding(
            AbsenceHoursSetupFindingCode.DesiredHoursNeedChange,
            DescribeDesiredChange(calculation, desired, dailyHours, weekdayCredit) + ownRule,
            DesiredChangePlace(calculation)));
    }

    private static string DescribeDesiredChange(AbsenceHoursCalculation calculation, decimal desired, decimal dailyHours, decimal currentCredit)
    {
        var data = calculation.ContractData;
        var dailyPlace = data.DefaultWorkingHoursSource == ContractValueSource.SchedulingRule ? "scheduling rule" : "company work settings";

        if (dailyHours <= decimal.Zero)
        {
            return $"A weekday absence credits {currentCredit} h instead of {Round(desired)} h because the daily working hours are 0; set them in the {dailyPlace}.";
        }

        var requiredPercent = Round(desired / dailyHours * MonthlyTargetHoursConstants.FullWorkloadPercent);
        var intro = $"A weekday absence credits {currentCredit} h instead of {Round(desired)} h. With {dailyHours} h per day that needs a workload of {requiredPercent} %: ";

        return data.WorkloadPercentSource switch
        {
            WorkloadPercentSource.ContractPercent or WorkloadPercentSource.ContractPercentDefault =>
                intro + $"set the contract's workload percent field to {requiredPercent}.",
            WorkloadPercentSource.ContractRatio or WorkloadPercentSource.SchedulingRuleRatio when data.WorkloadRatioBasis is { } basis =>
                intro + $"set the guaranteed hours to {Round(basis.FullTime * requiredPercent / MonthlyTargetHoursConstants.FullWorkloadPercent)} " +
                $"at a full time of {basis.FullTime} (in the {(data.WorkloadPercentSource == WorkloadPercentSource.SchedulingRuleRatio ? "scheduling rule" : "contract")})" +
                DailyHoursAlternative(data, desired, dailyPlace),
            WorkloadPercentSource.NoContractFullWorkload or WorkloadPercentSource.MonthlyTargetHoursFullWorkload =>
                intro + $"assign a contract with that workload share (percent field {requiredPercent}, or guaranteed hours " +
                $"{Round(data.FullTime * requiredPercent / MonthlyTargetHoursConstants.FullWorkloadPercent)} at a full time of {data.FullTime}); " +
                "without a contract every employee counts as full workload.",
            _ => intro + "fix the implausible workload basis first (positive full time, ratio within the plausibility ceiling)."
        };
    }

    // With 0 guaranteed hours the share is 0 %, and no change of the daily hours can reach the wish; only the
    // guaranteed hours can.
    private static string DailyHoursAlternative(EffectiveContractData data, decimal desired, string dailyPlace) =>
        data.WorkloadPercent > decimal.Zero
            ? $", or change the daily working hours in the {dailyPlace} to {Round(desired * MonthlyTargetHoursConstants.FullWorkloadPercent / data.WorkloadPercent)}."
            : "; with 0 guaranteed hours no daily-hours value can reach the wish.";

    private static string DesiredChangePlace(AbsenceHoursCalculation calculation) =>
        calculation.ContractData.WorkloadPercentSource switch
        {
            WorkloadPercentSource.NoContractFullWorkload or WorkloadPercentSource.MonthlyTargetHoursFullWorkload => AssignmentPlace,
            WorkloadPercentSource.SchedulingRuleRatio => SchedulingRulePlace,
            _ => ContractPlace
        };

    // Only employees who read the monthly table carry the year's rows and workdays (service fills them); for them a
    // partial table switches the basis from month to month, and a month value far from workdays x daily hours makes a
    // full month of vacation miss the month's target. Both point to the same cure: fill every month, or use a fixed
    // contract (Weg 1).
    private static void AddMonthlyTableFindings(AbsenceHoursCalculation calculation, List<AbsenceHoursSetupFinding> findings)
    {
        if (calculation.MonthlyTargetHoursOfYear is not { } rows || calculation.WorkdaysPerMonth is not { } workdays)
        {
            return;
        }

        if (MonthlyTableAnalyzer.IsPartiallyFilled(rows))
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.MonthlyRowsIncomplete,
                $"The monthly target hours table of {calculation.Date.Year} has rows for {rows.Count} of 12 months (missing: " +
                $"{string.Join(", ", MonthlyTableAnalyzer.MissingMonths(rows))}). In those months the basis switches from the month value to the " +
                $"{(calculation.ContractData.HasActiveContract ? "contract or settings value" : "settings value")}. " +
                "Fill every month, or use a contract with fixed guaranteed hours.",
                MonthlyTargetHoursPlace));
        }

        var deviating = MonthlyTableAnalyzer.DeviatingMonths(rows, workdays, calculation.ContractData.DefaultWorkingHours);
        if (deviating.Count > 0)
        {
            findings.Add(new AbsenceHoursSetupFinding(
                AbsenceHoursSetupFindingCode.MonthlyRowDeviatesFromWorkdays,
                $"Monthly values deviate by more than {MonthlyTableAnalyzer.DeviationThresholdPercent} % from workdays x daily hours: {string.Join("; ", deviating)}. " +
                "A full month of vacation would then not equal the month's target. Correct the month values, or use a contract with fixed guaranteed hours.",
                MonthlyTargetHoursPlace));
        }
    }

    private static string Describe(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "inherited";

    private static string NameInterval(int? interval) =>
        interval.HasValue && Enum.IsDefined(typeof(PaymentInterval), interval.Value)
            ? ((PaymentInterval)interval.Value).ToString()
            : "none";

    // Normalised so a whole number prints as "75", not "75.00".
    private static decimal Round(decimal value) =>
        Math.Round(value, Decimals, MidpointRounding.AwayFromZero) / NormalisingOne;

    private const decimal NormalisingOne = 1.000000000000000000000000000m;
}
