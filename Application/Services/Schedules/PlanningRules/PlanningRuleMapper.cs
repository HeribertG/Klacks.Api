// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure mapping of the persisted rule rows onto the engine-neutral PlanRule records. CounterRule keeps its
/// warn/block model: the row's own Enforcement override, else the global counterRule mode, decides the
/// severity (Block = Hard, Warn = Soft with DefaultCounterRuleSoftWeight). Domain enums map one to one onto the
/// optimizer enums; an unknown value throws instead of silently picking a default.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.ScheduleOptimizer.Constraints.Rules;

namespace Klacks.Api.Application.Services.Schedules.PlanningRules;

public static class PlanningRuleMapper
{
    public static RuleSeverity ToSeverity(RuleEnforcementMode mode) => mode switch
    {
        RuleEnforcementMode.Block => RuleSeverity.Hard,
        RuleEnforcementMode.Warn => RuleSeverity.Soft,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown enforcement mode."),
    };

    public static RuleSeverity ToSeverity(PlanningConstraintSeverity severity) => severity switch
    {
        PlanningConstraintSeverity.Hard => RuleSeverity.Hard,
        PlanningConstraintSeverity.Soft => RuleSeverity.Soft,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown constraint severity."),
    };

    public static RuleShiftKind ToShiftKind(PlanningShiftKind kind) => kind switch
    {
        PlanningShiftKind.Work => RuleShiftKind.Work,
        PlanningShiftKind.Early => RuleShiftKind.Early,
        PlanningShiftKind.Late => RuleShiftKind.Late,
        PlanningShiftKind.Night => RuleShiftKind.Night,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown shift kind."),
    };

    public static FairnessMetric ToMetric(PlanningFairnessMetric metric) => metric switch
    {
        PlanningFairnessMetric.NightDays => FairnessMetric.NightDays,
        PlanningFairnessMetric.WeekendDays => FairnessMetric.WeekendDays,
        PlanningFairnessMetric.WorkedDays => FairnessMetric.WorkedDays,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unknown fairness metric."),
    };

    public static FairnessWindow ToWindow(PlanningFairnessWindow window) => window switch
    {
        PlanningFairnessWindow.PlanPeriod => FairnessWindow.PlanPeriod,
        PlanningFairnessWindow.Week => FairnessWindow.Week,
        PlanningFairnessWindow.Month => FairnessWindow.Month,
        _ => throw new ArgumentOutOfRangeException(nameof(window), window, "Unknown fairness window."),
    };

    public static RuleCounterEvent ToCounterEvent(CounterEventType eventType) => eventType switch
    {
        CounterEventType.NightShift => RuleCounterEvent.NightShift,
        CounterEventType.WorkedDayInWeek => RuleCounterEvent.WorkedDayInWeek,
        CounterEventType.ShiftExceedingHours => RuleCounterEvent.ShiftExceedingHours,
        _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Unknown counter event."),
    };

    public static RuleCalendarPeriod ToCalendarPeriod(CounterPeriod period) => period switch
    {
        CounterPeriod.Week => RuleCalendarPeriod.Week,
        CounterPeriod.Month => RuleCalendarPeriod.Month,
        CounterPeriod.Year => RuleCalendarPeriod.Year,
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown counter period."),
    };

    public static PeriodCountRule FromCounterRule(CounterRule rule, RuleEnforcementMode globalMode, IReadOnlySet<string>? agentScope)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new PeriodCountRule(
            RuleId: rule.Id,
            Severity: ToSeverity(rule.Enforcement ?? globalMode),
            Weight: PlanningConstraintDefaults.DefaultCounterRuleSoftWeight,
            Event: ToCounterEvent(rule.EventType),
            Period: ToCalendarPeriod(rule.Period),
            Threshold: rule.Threshold,
            HoursThreshold: rule.HoursThreshold,
            AgentScope: agentScope);
    }

    public static PlanRule FromConstraint(PlanningConstraint constraint, PlanningConstraintParameters parameters, IReadOnlySet<string>? agentScope)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        ArgumentNullException.ThrowIfNull(parameters);
        if (parameters.Kind != constraint.Kind)
        {
            throw new ArgumentException("The parameters do not belong to the constraint kind.", nameof(parameters));
        }

        var severity = ToSeverity(constraint.Severity);
        return parameters switch
        {
            MaxConsecutiveOfKindParameters run => new MaxConsecutiveOfKindRule(
                constraint.Id, severity, constraint.Weight, ToShiftKind(run.ShiftKind), run.MaxRun, agentScope),
            ForbiddenTransitionParameters transition => new ForbiddenTransitionRule(
                constraint.Id, severity, constraint.Weight, ToShiftKind(transition.FromKind), ToShiftKind(transition.ToKind),
                transition.WithinDays, agentScope),
            RestAfterKindParameters rest => new RestAfterKindRule(
                constraint.Id, severity, constraint.Weight, ToShiftKind(rest.ShiftKind), rest.FreeDays, agentScope),
            TeamFairnessParameters fairness => new TeamFairnessRule(
                constraint.Id, constraint.Weight, ToMetric(fairness.Metric), ToWindow(fairness.Window), fairness.MaxSpread,
                fairness.ProRata, fairness.WeekendDays, agentScope),
            _ => throw new ArgumentOutOfRangeException(nameof(parameters), parameters.Kind, "Unknown constraint parameters."),
        };
    }
}
