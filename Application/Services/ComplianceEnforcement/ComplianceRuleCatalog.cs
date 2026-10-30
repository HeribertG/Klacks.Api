// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The compliance rules whose warn/block reaction can be changed, and the validation message keys each one governs.
/// The five timeline rules take their keys from ComplianceEscalationService.CommentToRuleName (one source), the
/// others escalate inside their own evaluators and are listed here. The approaching-cap pre-warnings are not listed
/// because they stay warnings under Block; the planning-rule family has no mode (its severity comes from the
/// constraint row) and is not a rule of this catalog.
/// </summary>

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.ComplianceEnforcement;

public static class ComplianceRuleCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> KeysByRule = BuildKeysByRule();

    private static readonly IReadOnlySet<string> PeriodWideRules = new HashSet<string>
    {
        ComplianceRuleNames.PeriodCap,
        ComplianceRuleNames.RollingAverage,
        ComplianceRuleNames.RestDayRotation,
        ComplianceRuleNames.CounterRule,
        ComplianceRuleNames.CompensatoryRest,
    };

    private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [ComplianceRuleNames.MaxDailyHours] = "maximum daily hours",
        [ComplianceRuleNames.MaxWeeklyHours] = "maximum weekly hours",
        [ComplianceRuleNames.MinRestHours] = "minimum rest between duties",
        [ComplianceRuleNames.MinRestDays] = "minimum rest days",
        [ComplianceRuleNames.MaxConsecutiveDays] = "maximum days in a row",
        [ComplianceRuleNames.PeriodCap] = "period ceilings",
        [ComplianceRuleNames.RollingAverage] = "rolling weekly average",
        [ComplianceRuleNames.RestDayRotation] = "rest day rotation",
        [ComplianceRuleNames.CounterRule] = "counting rules",
        [ComplianceRuleNames.CompensatoryRest] = "owed time off",
        [ComplianceRuleNames.RestrictedTimeWindow] = "blocked times of the day",
        [ComplianceRuleNames.HolidayWork] = "work on public holidays",
    };

    public static IReadOnlyList<string> Rules { get; } =
    [
        ComplianceRuleNames.MaxDailyHours,
        ComplianceRuleNames.MaxWeeklyHours,
        ComplianceRuleNames.MinRestHours,
        ComplianceRuleNames.MinRestDays,
        ComplianceRuleNames.MaxConsecutiveDays,
        ComplianceRuleNames.PeriodCap,
        ComplianceRuleNames.RollingAverage,
        ComplianceRuleNames.RestDayRotation,
        ComplianceRuleNames.CounterRule,
        ComplianceRuleNames.CompensatoryRest,
        ComplianceRuleNames.RestrictedTimeWindow,
        ComplianceRuleNames.HolidayWork,
    ];

    /// <summary>
    /// The stored spelling of a rule name for any casing or surrounding whitespace; null when it is no rule of the catalog.
    /// </summary>
    /// <param name="raw">Rule name as typed, e.g. from a skill parameter</param>
    public static string? Normalize(string? raw)
    {
        var trimmed = raw?.Trim();
        return Rules.FirstOrDefault(rule => string.Equals(rule, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Plain-language name of a rule for sentences; the rule name itself when it is no rule of the catalog.
    /// </summary>
    /// <param name="rule">A rule name of the catalog</param>
    public static string Label(string rule) => Labels.GetValueOrDefault(rule) ?? rule;

    /// <summary>
    /// The message keys of the findings that escalate from Warning to Error when the rule is set to Block.
    /// </summary>
    /// <param name="rule">A rule name of the catalog</param>
    public static IReadOnlyList<string> IssueKeys(string rule) => KeysByRule.GetValueOrDefault(rule) ?? [];

    /// <summary>
    /// True for rules the period check evaluates per cap period, rolling window or as of the window end instead of
    /// day by day, so its finding count covers only what that check reports.
    /// </summary>
    /// <param name="rule">A rule name of the catalog</param>
    public static bool IsEvaluatedPerPeriod(string rule) => PeriodWideRules.Contains(rule);

    /// <summary>
    /// True when single counting rules can carry their own reaction that wins over the setting of the rule.
    /// </summary>
    /// <param name="rule">A rule name of the catalog</param>
    public static bool CanBeOverriddenPerItem(string rule) => rule == ComplianceRuleNames.CounterRule;

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildKeysByRule()
    {
        var keys = ComplianceEscalationService.CommentToRuleName
            .GroupBy(pair => pair.Value, pair => pair.Key)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.ToList());

        keys[ComplianceRuleNames.PeriodCap] =
        [
            ScheduleValidationKeys.PeriodCap,
            ScheduleValidationKeys.PeriodCapOvertime,
            ScheduleValidationKeys.PeriodCapOvertimeWindow,
        ];
        keys[ComplianceRuleNames.RollingAverage] = [ScheduleValidationKeys.RollingAverage];
        keys[ComplianceRuleNames.RestDayRotation] = [ScheduleValidationKeys.RestDayRotation];
        keys[ComplianceRuleNames.CounterRule] = [ScheduleValidationKeys.CounterRule];
        keys[ComplianceRuleNames.CompensatoryRest] =
        [
            ScheduleValidationKeys.CompensatoryRestDue,
            ScheduleValidationKeys.CompensatoryRestOverdue,
        ];
        keys[ComplianceRuleNames.RestrictedTimeWindow] = [ScheduleValidationKeys.RestrictedTimeWindow];
        keys[ComplianceRuleNames.HolidayWork] = [ScheduleValidationKeys.HolidayWork];

        return keys;
    }
}
