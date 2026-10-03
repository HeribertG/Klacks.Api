// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="ICounterRuleEvaluator"/> (K18), an adapter over the shared planning-rule evaluator
/// (Klacks.ScheduleOptimizer PeriodCountRule). For every active rule and candidate date the calendar period
/// containing that date (ISO week / month / year) is loaded from the client's Work rows plus the not-yet-persisted
/// planned slots; the candidate date becomes the one-day evaluation period and every other segment of the
/// calendar period its carry-in, so the shared evaluator counts exactly the whole period and reports at the
/// candidate date. Only the first candidate date of a period is evaluated per rule, so periods without a
/// candidate date are never reported. Warning escalates to Error when the effective enforcement mode is Block:
/// the global counterRule mode is resolved once per evaluation, but each rule's own
/// <see cref="CounterRule.Enforcement"/> override (when set) wins over it for that rule only.
/// </summary>
/// <param name="ruleRepository">Reads the active CounterRule set</param>
/// <param name="context">Database access for the client's Work rows in the counted period</param>
/// <param name="enforcementResolver">Resolves warn/block for the counterRule compliance rule</param>
/// <param name="contractDataProvider">Resolves the client's active SchedulingRule (industry scoping) and effective night window</param>
/// <remarks>
/// Night-shift counting uses the client's effective surcharge night window from
/// EffectiveContractData - the full K2 chain (SchedulingRule -> Contract -> settings -> default)
/// resolved by ClientContractDataProvider, never a re-implemented subset of it. Both the work segment
/// and the window wrap midnight, so a Saturday 22:00-07:00 shift counts against a 23:00-06:00 window.
/// Counting is calendar-anchored (never rolling) in this stage; the surcharge-applying action ("pay
/// extra from the 25th night on") is a documented later stage - this evaluator only warns/blocks.
/// Known limits of the shared counter (pinned by tests): ShiftExceedingHours compares whole minutes and keeps
/// the four longest segments of a day.
/// </remarks>

using System.Globalization;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules.PlanningRules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Constraints.Rules;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class CounterRuleEvaluator : ICounterRuleEvaluator
{
    private const int DaysPerWeek = 7;
    private const string EvaluationAgentId = "counter-rule-client";

    private readonly ICounterRuleRepository _ruleRepository;
    private readonly DataBaseContext _context;
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly IClientContractDataProvider _contractDataProvider;

    public CounterRuleEvaluator(
        ICounterRuleRepository ruleRepository,
        DataBaseContext context,
        IComplianceEnforcementResolver enforcementResolver,
        IClientContractDataProvider contractDataProvider)
    {
        _ruleRepository = ruleRepository;
        _context = context;
        _enforcementResolver = enforcementResolver;
        _contractDataProvider = contractDataProvider;
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        return await EvaluateCoreAsync(clientId, clientName, [asOfDate], [], analyseToken, cancellationToken);
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)> plannedSlots,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (plannedSlots.Count == 0)
        {
            return [];
        }

        var candidateDates = plannedSlots.Select(s => s.Date).Distinct().ToList();
        return await EvaluateCoreAsync(clientId, clientName, candidateDates, plannedSlots, analyseToken, cancellationToken);
    }

    private async Task<List<ScheduleValidationNotificationDto>> EvaluateCoreAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<DateOnly> candidateDates,
        IReadOnlyList<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)> plannedSlots,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var (rules, nightWindow) = await ResolveApplicableRulesAndNightWindowAsync(clientId, candidateDates.Max());
        if (rules.Count == 0)
        {
            return [];
        }

        var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.CounterRule);

        var entries = new List<ScheduleValidationNotificationDto>();
        var reportedPeriods = new HashSet<(Guid RuleId, DateOnly PeriodStart)>();

        foreach (var rule in rules)
        {
            foreach (var date in candidateDates)
            {
                var (periodStart, periodEnd) = ResolvePeriod(rule.Period, date);
                if (!reportedPeriods.Add((rule.Id, periodStart)))
                {
                    continue;
                }

                var works = await LoadWorksAsync(clientId, periodStart, periodEnd, analyseToken, cancellationToken);
                var slotsInPeriod = plannedSlots
                    .Where(s => s.Date >= periodStart && s.Date <= periodEnd)
                    .Select(s => new WorkSegment(s.Date, s.StartTime, s.EndTime));

                var effectiveMode = rule.Enforcement ?? mode;
                var count = CountEvents(rule, effectiveMode, works.Concat(slotsInPeriod).ToList(), nightWindow, date);
                if (count < rule.Threshold)
                {
                    continue;
                }

                entries.Add(BuildEntry(clientId, clientName, date, rule, count, effectiveMode));
            }
        }

        return entries;
    }

    private sealed record WorkSegment(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);

    // The candidate date is the one-day evaluation period: its segments form the plan, every other segment of
    // the calendar period is carry-in. Without a finding the count stayed below the threshold; the shared
    // evaluator reports the full count as Observed once it is reached.
    private static int CountEvents(
        CounterRule rule,
        RuleEnforcementMode mode,
        IReadOnlyList<WorkSegment> segments,
        (TimeOnly Start, TimeOnly End) nightWindow,
        DateOnly candidateDate)
    {
        var agent = new RuleAgent(EvaluationAgentId, new CoreNightWindow(nightWindow.Start, nightWindow.End), RuleTimeConstants.FullWorkloadPercent);
        var ruleSegments = segments
            .Select(s => new RuleSegment(EvaluationAgentId, s.Date, s.StartTime, s.EndTime, RuleTimeConstants.UnknownShiftTypeIndex, 0m))
            .ToList();
        var context = new RuleEvaluationContext(candidateDate, candidateDate, [agent], ruleSegments);
        var plan = new RulePlan(context);
        foreach (var segment in ruleSegments)
        {
            plan.TryAdd(segment);
        }

        var periodCountRule = PlanningRuleMapper.FromCounterRule(rule, mode, agentScope: null);
        var evaluation = PlanRuleEvaluatorFactory.Create([periodCountRule], context).Evaluate(plan);
        return evaluation.Findings.Count == 0 ? 0 : (int)evaluation.Findings[0].Observed;
    }

    private static (DateOnly Start, DateOnly End) ResolvePeriod(CounterPeriod period, DateOnly date)
    {
        switch (period)
        {
            case CounterPeriod.Week:
                var monday = date.AddDays(-(((int)date.DayOfWeek + 6) % DaysPerWeek));
                return (monday, monday.AddDays(DaysPerWeek - 1));
            case CounterPeriod.Month:
                var monthStart = new DateOnly(date.Year, date.Month, 1);
                return (monthStart, monthStart.AddMonths(1).AddDays(-1));
            case CounterPeriod.Year:
                return (new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31));
            default:
                throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown counter period.");
        }
    }

    private async Task<List<WorkSegment>> LoadWorksAsync(
        Guid clientId,
        DateOnly periodStart,
        DateOnly periodEnd,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var works = await _context.Work
            .AsNoTracking()
            .Where(w => w.ClientId == clientId
                && !w.IsDeleted
                && w.AnalyseToken == analyseToken
                && w.CurrentDate >= periodStart
                && w.CurrentDate <= periodEnd)
            .Select(w => new { w.CurrentDate, w.StartTime, w.EndTime })
            .ToListAsync(cancellationToken);

        return works.Select(w => new WorkSegment(w.CurrentDate, w.StartTime, w.EndTime)).ToList();
    }

    // One contract resolution serves BOTH concerns: industry scoping (SchedulingRuleId) and the K2
    // night window (NightStart/NightEnd already resolved rule -> contract -> settings -> default by
    // ClientContractDataProvider). The call is skipped only when no rule needs either - i.e. all rules
    // are global AND none counts night shifts.
    private async Task<(List<CounterRule> Rules, (TimeOnly Start, TimeOnly End) NightWindow)> ResolveApplicableRulesAndNightWindowAsync(
        Guid clientId, DateOnly asOfDate)
    {
        var defaultWindow = (
            ParseTimeOrDefault(SurchargeDefaults.NightStart, SurchargeDefaults.NightStart),
            ParseTimeOrDefault(SurchargeDefaults.NightEnd, SurchargeDefaults.NightEnd));

        var rules = await _ruleRepository.GetAllActiveAsync();
        if (rules.Count == 0)
        {
            return (rules, defaultWindow);
        }

        var needsContract = rules.Any(r => r.SchedulingRuleId != null)
            || rules.Any(r => r.EventType == CounterEventType.NightShift);
        if (!needsContract)
        {
            return (rules, defaultWindow);
        }

        var effectiveData = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, asOfDate);
        var nightWindow = (
            ParseTimeOrDefault(effectiveData.NightStart, SurchargeDefaults.NightStart),
            ParseTimeOrDefault(effectiveData.NightEnd, SurchargeDefaults.NightEnd));

        var applicable = rules
            .Where(r => r.SchedulingRuleId == null || r.SchedulingRuleId == effectiveData.SchedulingRuleId)
            .ToList();
        return (applicable, nightWindow);
    }

    private static TimeOnly ParseTimeOrDefault(string? value, string fallback)
    {
        return TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : TimeOnly.Parse(fallback, CultureInfo.InvariantCulture);
    }

    private static ScheduleValidationNotificationDto BuildEntry(
        Guid clientId,
        string clientName,
        DateOnly reportDate,
        CounterRule rule,
        int count,
        RuleEnforcementMode mode)
    {
        var isBlocked = mode == RuleEnforcementMode.Block;
        var commentParams = new Dictionary<string, string>
        {
            ["event"] = rule.EventType.ToString(),
            ["count"] = count.ToString(CultureInfo.InvariantCulture),
            ["threshold"] = rule.Threshold.ToString(CultureInfo.InvariantCulture),
            ["period"] = rule.Period.ToString(),
        };
        if (isBlocked)
        {
            commentParams[ComplianceRuleNames.EnforcementRuleParamKey] = ComplianceRuleNames.CounterRule;
        }

        return new ScheduleValidationNotificationDto
        {
            Type = isBlocked ? ScheduleValidationType.Error : ScheduleValidationType.Warning,
            ClientId = clientId,
            ClientName = clientName,
            Date = reportDate,
            Comment = ScheduleValidationKeys.CounterRule,
            CommentParams = commentParams,
        };
    }
}
