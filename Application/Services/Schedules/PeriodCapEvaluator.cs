// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IPeriodCapEvaluator"/>. Active PeriodCapRule rows are dispatched into two
/// independent evaluation modes, based on which field group is populated (see PeriodCapRule):
/// fixed-period rules (K5 stage 1: TotalHours scope) resolve the Month/Quarter/Year window each rule
/// covers, sum the client's persisted period hours via IPeriodHoursService, optionally add a
/// not-yet-persisted delta, and report every rule/window combination the projected total exceeds.
/// Rolling-average rules (K6) compare the average weekly hours over a trailing RollingWindowWeeks window
/// ending on each evaluated day against MaxAverageWeeklyHours, clamping the window to the client's
/// Membership.ValidFrom so a recent starter's average is never diluted by non-existent pre-employment
/// weeks. Both modes escalate Warning to Error when their respective compliance rule's enforcement mode
/// is Block (PeriodCap / RollingAverage). WarnAtPercent is validated on import but not yet evaluated here.
/// </summary>
/// <param name="ruleRepository">Reads the active PeriodCapRule set</param>
/// <param name="periodHoursService">Sums a client's persisted work/break hours for a date range</param>
/// <param name="enforcementResolver">Resolves warn/block for the PeriodCap and RollingAverage compliance rules</param>
/// <param name="membershipStartResolver">Resolves a client's employment start date for the K6 window clamp</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class PeriodCapEvaluator : IPeriodCapEvaluator
{
    private const int DaysPerWeek = 7;

    private readonly IPeriodCapRuleRepository _ruleRepository;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly IClientMembershipStartResolver _membershipStartResolver;

    public PeriodCapEvaluator(
        IPeriodCapRuleRepository ruleRepository,
        IPeriodHoursService periodHoursService,
        IComplianceEnforcementResolver enforcementResolver,
        IClientMembershipStartResolver membershipStartResolver)
    {
        _ruleRepository = ruleRepository;
        _periodHoursService = periodHoursService;
        _enforcementResolver = enforcementResolver;
        _membershipStartResolver = membershipStartResolver;
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        return await EvaluatePlannedAsync(clientId, clientName, [(asOfDate, 0m)], analyseToken, cancellationToken);
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (plannedHours.Count == 0)
        {
            return [];
        }

        var rules = await _ruleRepository.GetAllActiveAsync();
        var rollingAverageRules = rules.Where(IsRollingAverageRule).ToList();
        var fixedPeriodRules = rules.Where(r => !IsRollingAverageRule(r) && r.Scope == PeriodCapScope.TotalHours).ToList();

        if (fixedPeriodRules.Count == 0 && rollingAverageRules.Count == 0)
        {
            return [];
        }

        var entries = new List<ScheduleValidationNotificationDto>();

        if (fixedPeriodRules.Count > 0)
        {
            var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.PeriodCap);
            entries.AddRange(await EvaluateFixedPeriodRulesAsync(
                clientId, clientName, plannedHours, fixedPeriodRules, mode, analyseToken, cancellationToken));
        }

        if (rollingAverageRules.Count > 0)
        {
            var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.RollingAverage);
            var membershipStart = await _membershipStartResolver.GetValidFromAsync(clientId);
            entries.AddRange(await EvaluateRollingAverageRulesAsync(
                clientId, clientName, plannedHours, rollingAverageRules, membershipStart, mode, analyseToken, cancellationToken));
        }

        return entries;
    }

    private static bool IsRollingAverageRule(PeriodCapRule rule) =>
        rule.RollingWindowWeeks.HasValue && rule.MaxAverageWeeklyHours.HasValue;

    private async Task<List<ScheduleValidationNotificationDto>> EvaluateFixedPeriodRulesAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        List<PeriodCapRule> fixedPeriodRules,
        RuleEnforcementMode mode,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var entries = new List<ScheduleValidationNotificationDto>();

        foreach (var rule in fixedPeriodRules)
        {
            var windowGroups = plannedHours
                .Select(p => (Window: ResolveFixedPeriodBoundaries(rule, p.Date), p.Date, p.Hours))
                .GroupBy(x => x.Window);

            foreach (var group in windowGroups)
            {
                var (start, end) = group.Key;
                var additionalHours = group.Sum(x => x.Hours);
                var baseline = await _periodHoursService.CalculatePeriodHoursAsync(clientId, start, end, analyseToken);
                var projectedHours = baseline.Hours + additionalHours;
                if (projectedHours <= rule.CapHours)
                {
                    continue;
                }

                var reportDate = group.Min(x => x.Date);
                entries.Add(BuildFixedPeriodEntry(clientId, clientName, reportDate, rule, projectedHours, mode));
            }
        }

        return entries;
    }

    private async Task<List<ScheduleValidationNotificationDto>> EvaluateRollingAverageRulesAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        List<PeriodCapRule> rollingAverageRules,
        DateOnly? membershipStart,
        RuleEnforcementMode mode,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var entries = new List<ScheduleValidationNotificationDto>();
        var candidateDates = plannedHours.Select(p => p.Date).Distinct().OrderBy(d => d).ToList();

        foreach (var rule in rollingAverageRules)
        {
            foreach (var date in candidateDates)
            {
                var (windowStart, effectiveWeeks) = ResolveRollingWindow(rule, date, membershipStart);
                if (effectiveWeeks <= 0)
                {
                    // Not enough employment history yet to form even one complete week - averaging over
                    // a shorter-than-a-week span would either divide by (near) zero or, if padded with
                    // zero-hour weeks, silently hide a real breach. Skip until a full week exists.
                    continue;
                }

                var additionalHours = plannedHours
                    .Where(p => p.Date >= windowStart && p.Date <= date)
                    .Sum(p => p.Hours);

                var baseline = await _periodHoursService.CalculatePeriodHoursAsync(clientId, windowStart, date, analyseToken);
                var projectedAverage = (baseline.Hours + additionalHours) / effectiveWeeks;
                if (projectedAverage <= rule.MaxAverageWeeklyHours!.Value)
                {
                    continue;
                }

                entries.Add(BuildRollingAverageEntry(clientId, clientName, date, rule, projectedAverage, mode));
            }
        }

        return entries;
    }

    // The naive window is [date - RollingWindowWeeks*7 + 1, date]. Clamped to Membership.ValidFrom so a
    // recently started client is averaged only over the weeks actually worked, never over non-existent
    // pre-employment weeks (padding those with zero would artificially lower the average and hide a real
    // breach). effectiveWeeks is the number of COMPLETE weeks in the clamped window (floor division) - a
    // partial trailing week is deliberately excluded rather than counted as a fraction, which is the
    // conservative direction (it can only inflate, never hide, a violation).
    private static (DateOnly WindowStart, int EffectiveWeeks) ResolveRollingWindow(
        PeriodCapRule rule, DateOnly date, DateOnly? membershipStart)
    {
        var windowWeeks = rule.RollingWindowWeeks!.Value;
        var naiveStart = date.AddDays(-((windowWeeks * DaysPerWeek) - 1));
        var effectiveStart = membershipStart.HasValue && membershipStart.Value > naiveStart
            ? membershipStart.Value
            : naiveStart;

        if (effectiveStart > date)
        {
            return (effectiveStart, 0);
        }

        var effectiveDays = date.DayNumber - effectiveStart.DayNumber + 1;
        return (effectiveStart, effectiveDays / DaysPerWeek);
    }

    private static ScheduleValidationNotificationDto BuildFixedPeriodEntry(
        Guid clientId,
        string clientName,
        DateOnly reportDate,
        PeriodCapRule rule,
        decimal projectedHours,
        RuleEnforcementMode mode)
    {
        var isBlocked = mode == RuleEnforcementMode.Block;
        var commentParams = new Dictionary<string, string>
        {
            ["actualHours"] = projectedHours.ToString("F1", CultureInfo.InvariantCulture),
            ["capHours"] = rule.CapHours.ToString("F0", CultureInfo.InvariantCulture),
            ["period"] = rule.Period.ToString(),
        };
        if (isBlocked)
        {
            commentParams[ComplianceRuleNames.EnforcementRuleParamKey] = ComplianceRuleNames.PeriodCap;
        }

        return new ScheduleValidationNotificationDto
        {
            Type = isBlocked ? ScheduleValidationType.Error : ScheduleValidationType.Warning,
            ClientId = clientId,
            ClientName = clientName,
            Date = reportDate,
            Comment = ScheduleValidationKeys.PeriodCap,
            CommentParams = commentParams,
        };
    }

    private static ScheduleValidationNotificationDto BuildRollingAverageEntry(
        Guid clientId,
        string clientName,
        DateOnly reportDate,
        PeriodCapRule rule,
        decimal projectedAverage,
        RuleEnforcementMode mode)
    {
        var isBlocked = mode == RuleEnforcementMode.Block;
        var commentParams = new Dictionary<string, string>
        {
            ["actualHours"] = projectedAverage.ToString("F1", CultureInfo.InvariantCulture),
            ["capHours"] = rule.MaxAverageWeeklyHours!.Value.ToString("F0", CultureInfo.InvariantCulture),
            ["windowWeeks"] = rule.RollingWindowWeeks!.Value.ToString(CultureInfo.InvariantCulture),
        };
        if (isBlocked)
        {
            commentParams[ComplianceRuleNames.EnforcementRuleParamKey] = ComplianceRuleNames.RollingAverage;
        }

        return new ScheduleValidationNotificationDto
        {
            Type = isBlocked ? ScheduleValidationType.Error : ScheduleValidationType.Warning,
            ClientId = clientId,
            ClientName = clientName,
            Date = reportDate,
            Comment = ScheduleValidationKeys.RollingAverage,
            CommentParams = commentParams,
        };
    }

    private static (DateOnly Start, DateOnly End) ResolveFixedPeriodBoundaries(PeriodCapRule rule, DateOnly date)
    {
        switch (rule.Period)
        {
            case PeriodCapPeriod.Month:
                var monthStart = new DateOnly(date.Year, date.Month, 1);
                return (monthStart, monthStart.AddMonths(1).AddDays(-1));

            case PeriodCapPeriod.Quarter:
                var quarterStartMonth = (((date.Month - 1) / 3) * 3) + 1;
                var quarterStart = new DateOnly(date.Year, quarterStartMonth, 1);
                return (quarterStart, quarterStart.AddMonths(3).AddDays(-1));

            case PeriodCapPeriod.Year:
                return (new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31));

            case PeriodCapPeriod.CustomWeeks:
                if (!rule.CustomPeriodWeeks.HasValue || rule.CustomPeriodWeeks.Value <= 0)
                {
                    throw new NotSupportedException(
                        "PeriodCapRule with period CustomWeeks requires CustomPeriodWeeks to be set; CustomWeeks is not yet importable via region-setup.json in this stage.");
                }

                var windowDays = rule.CustomPeriodWeeks.Value * DaysPerWeek;
                return (date.AddDays(-(windowDays - 1)), date);

            default:
                throw new ArgumentOutOfRangeException(nameof(rule), rule.Period, "Unknown period cap period.");
        }
    }
}
