// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IPeriodCapEvaluator"/>. Active PeriodCapRule rows are dispatched into three
/// independent evaluation modes, based on which field group is populated (see PeriodCapRule):
/// fixed-period rules (K5 stage 1: TotalHours scope) resolve the Month/Quarter/Year window each rule
/// covers, sum the client's persisted period hours via IPeriodHoursService, optionally add a
/// not-yet-persisted delta, and report every rule/window combination the projected total exceeds.
/// Overtime-cap rules (OvertimeHours scope) cap only the OVERTIME within the same windows: the sum over
/// base units (day or week, per the system's single overtime definition resolved via
/// IOvertimeConfigResolver) of max(0, actual hours per unit - overtime threshold), fed exclusively from
/// Work rows via IClientWorkHoursProvider. Rolling-average rules (K6) compare the average weekly hours
/// over a trailing RollingWindowWeeks window ending on each evaluated day against MaxAverageWeeklyHours,
/// clamping the window to the client's Membership.ValidFrom so a recent starter's average is never
/// diluted by non-existent pre-employment weeks. All modes escalate Warning to Error when their
/// respective compliance rule's enforcement mode is Block (PeriodCap for both fixed scopes /
/// RollingAverage). WarnAtPercent is validated on import but not yet evaluated here.
/// </summary>
/// <param name="ruleRepository">Reads the active PeriodCapRule set</param>
/// <param name="periodHoursService">Sums a client's persisted work/break hours for a date range (TotalHours scope)</param>
/// <param name="enforcementResolver">Resolves warn/block for the PeriodCap and RollingAverage compliance rules</param>
/// <param name="membershipStartResolver">Resolves a client's employment start date for the K6 window clamp</param>
/// <param name="contractDataProvider">Resolves the client's active SchedulingRule for industry-scoped rules and the OvertimeThreshold fallback</param>
/// <param name="overtimeConfigResolver">Resolves the system's single overtime definition (basis + tier ladder) for the OvertimeHours scope</param>
/// <param name="workHoursProvider">Sums a client's persisted Work-only hours per day for the OvertimeHours scope</param>
/// <param name="weekConfiguration">Resolves the configured week start for week-basis overtime bucketing</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class PeriodCapEvaluator : IPeriodCapEvaluator
{
    private const int DaysPerWeek = 7;

    private readonly IPeriodCapRuleRepository _ruleRepository;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly IClientMembershipStartResolver _membershipStartResolver;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IOvertimeConfigResolver _overtimeConfigResolver;
    private readonly IClientWorkHoursProvider _workHoursProvider;
    private readonly IWeekConfiguration _weekConfiguration;

    public PeriodCapEvaluator(
        IPeriodCapRuleRepository ruleRepository,
        IPeriodHoursService periodHoursService,
        IComplianceEnforcementResolver enforcementResolver,
        IClientMembershipStartResolver membershipStartResolver,
        IClientContractDataProvider contractDataProvider,
        IOvertimeConfigResolver overtimeConfigResolver,
        IClientWorkHoursProvider workHoursProvider,
        IWeekConfiguration weekConfiguration)
    {
        _ruleRepository = ruleRepository;
        _periodHoursService = periodHoursService;
        _enforcementResolver = enforcementResolver;
        _membershipStartResolver = membershipStartResolver;
        _contractDataProvider = contractDataProvider;
        _overtimeConfigResolver = overtimeConfigResolver;
        _workHoursProvider = workHoursProvider;
        _weekConfiguration = weekConfiguration;
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

        var latestCandidateDate = plannedHours.Max(p => p.Date);
        var rules = await ResolveApplicableRulesAsync(clientId, latestCandidateDate);
        var rollingAverageRules = rules.Where(IsRollingAverageRule).ToList();
        var fixedPeriodRules = rules.Where(r => !IsRollingAverageRule(r) && r.Scope == PeriodCapScope.TotalHours).ToList();
        var overtimeCapRules = rules.Where(r => !IsRollingAverageRule(r) && r.Scope == PeriodCapScope.OvertimeHours).ToList();

        if (fixedPeriodRules.Count == 0 && rollingAverageRules.Count == 0 && overtimeCapRules.Count == 0)
        {
            return [];
        }

        var entries = new List<ScheduleValidationNotificationDto>();

        if (fixedPeriodRules.Count > 0 || overtimeCapRules.Count > 0)
        {
            var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.PeriodCap);
            if (fixedPeriodRules.Count > 0)
            {
                entries.AddRange(await EvaluateFixedPeriodRulesAsync(
                    clientId, clientName, plannedHours, fixedPeriodRules, mode, analyseToken, cancellationToken));
            }

            if (overtimeCapRules.Count > 0)
            {
                entries.AddRange(await EvaluateOvertimeCapRulesAsync(
                    clientId, clientName, plannedHours, overtimeCapRules, latestCandidateDate, mode, analyseToken, cancellationToken));
            }
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

    // Industry scoping: a rule bound to a SchedulingRule applies only when the client's active contract
    // references that rule. The contract is resolved once per evaluation at the latest candidate date -
    // a contract change inside a window is deliberately not split per day (the window verdict follows
    // the contract that governs the evaluated end state).
    private async Task<List<PeriodCapRule>> ResolveApplicableRulesAsync(Guid clientId, DateOnly asOfDate)
    {
        var rules = await _ruleRepository.GetAllActiveAsync();
        if (rules.All(r => r.SchedulingRuleId == null))
        {
            return rules;
        }

        var effectiveData = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, asOfDate);
        return rules
            .Where(r => r.SchedulingRuleId == null || r.SchedulingRuleId == effectiveData.SchedulingRuleId)
            .ToList();
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

    // OvertimeHours scope: caps the overtime accrued inside the same fixed windows the TotalHours scope
    // uses (Month/Quarter/Year fixed, CustomWeeks trailing per candidate date). Overtime in a window is
    // the sum over base units (day or week) of max(0, unit hours - threshold). No membership clamp is
    // needed here (unlike the rolling average): before the employment start no Work rows exist, and a
    // plain sum is not diluted by empty units - they simply contribute zero.
    private async Task<List<ScheduleValidationNotificationDto>> EvaluateOvertimeCapRulesAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        List<PeriodCapRule> overtimeCapRules,
        DateOnly latestCandidateDate,
        RuleEnforcementMode mode,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var entries = new List<ScheduleValidationNotificationDto>();

        var definition = await ResolveOvertimeDefinitionAsync(clientId, latestCandidateDate);
        if (definition == null)
        {
            // Overtime is undefined for this client (no tier ladder and no positive contract threshold).
            // Skip the rules entirely - a silent threshold of 0 would declare EVERY worked hour overtime.
            return entries;
        }

        var (basis, threshold) = definition.Value;

        foreach (var rule in overtimeCapRules)
        {
            var windowGroups = plannedHours
                .Select(p => (Window: ResolveFixedPeriodBoundaries(rule, p.Date), p.Date))
                .GroupBy(x => x.Window);

            foreach (var group in windowGroups)
            {
                var (start, end) = group.Key;
                var overtimeHours = basis == OvertimeBasis.Week
                    ? await SumWeeklyOvertimeAsync(clientId, plannedHours, start, end, threshold, analyseToken, cancellationToken)
                    : await SumDailyOvertimeAsync(clientId, plannedHours, start, end, threshold, analyseToken);
                if (overtimeHours <= rule.CapHours)
                {
                    continue;
                }

                var reportDate = group.Min(x => x.Date);
                entries.Add(BuildOvertimeCapEntry(clientId, clientName, reportDate, rule, overtimeHours, mode));
            }
        }

        return entries;
    }

    // The system has exactly ONE overtime definition, owned by the surcharge engine's resolution chain
    // (SchedulingRule ladder -> dated revision snapshot -> OVERTIME_* settings -> contract
    // OvertimeThreshold). Tier 1's AfterHours marks where overtime BEGINS; higher tiers are only pay
    // grades of the same overtime and are irrelevant for the cap. Without any tier ladder, a positive
    // contract OvertimeThreshold still defines overtime - on a WEEK basis by project convention:
    // worktime.overtimeThreshold is a weekly threshold (cf. regions/se.json, defaultWorkingHours 8 per
    // day vs. overtimeThreshold 40 per week). Resolved ONCE per evaluation at the latest candidate date,
    // mirroring ResolveApplicableRulesAsync: a definition change inside a window is deliberately not
    // split per day - the window verdict follows the definition that governs the evaluated end state.
    private async Task<(OvertimeBasis Basis, decimal Threshold)?> ResolveOvertimeDefinitionAsync(Guid clientId, DateOnly asOfDate)
    {
        var config = await _overtimeConfigResolver.ResolveAsync(clientId, asOfDate);
        if (config.Tiers.Count > 0)
        {
            return (config.Basis, config.Tiers[0].AfterHours);
        }

        var effectiveData = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, asOfDate);
        if (effectiveData.OvertimeThreshold > 0)
        {
            return (OvertimeBasis.Week, effectiveData.OvertimeThreshold);
        }

        return null;
    }

    private async Task<decimal> SumDailyOvertimeAsync(
        Guid clientId,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        DateOnly windowStart,
        DateOnly windowEnd,
        decimal threshold,
        Guid? analyseToken)
    {
        var persisted = await _workHoursProvider.GetWorkHoursByDayAsync(clientId, windowStart, windowEnd, analyseToken);
        var hoursByDay = new Dictionary<DateOnly, decimal>(persisted);
        foreach (var (date, hours) in plannedHours)
        {
            if (date < windowStart || date > windowEnd)
            {
                continue;
            }

            hoursByDay[date] = hoursByDay.GetValueOrDefault(date) + hours;
        }

        return hoursByDay.Values.Sum(hours => Math.Max(0m, hours - threshold));
    }

    // Week attribution: a base week belongs to the window that contains its WEEK START. This tiles the
    // base weeks exactly over adjacent windows - every week is counted in precisely one window, never
    // twice and never dropped. A week that starts inside the window counts as a WHOLE week even when its
    // end lies past the window end, so the persisted-hours query range extends to that week's last day;
    // conversely, days at the window start belonging to a week that started in the previous window are
    // not read at all (that week is attributed to - and fully counted by - the previous window).
    private async Task<decimal> SumWeeklyOvertimeAsync(
        Guid clientId,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        DateOnly windowStart,
        DateOnly windowEnd,
        decimal threshold,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var weekStartOfWindowStart = await _weekConfiguration.GetWeekStartAsync(windowStart, cancellationToken);
        var firstAttributedWeekStart = weekStartOfWindowStart >= windowStart
            ? weekStartOfWindowStart
            : weekStartOfWindowStart.AddDays(DaysPerWeek);
        if (firstAttributedWeekStart > windowEnd)
        {
            return 0m;
        }

        var lastAttributedWeekStart = WeekStartOf(firstAttributedWeekStart, windowEnd);
        var queryEnd = lastAttributedWeekStart.AddDays(DaysPerWeek - 1);

        var persisted = await _workHoursProvider.GetWorkHoursByDayAsync(clientId, firstAttributedWeekStart, queryEnd, analyseToken);
        var hoursByWeek = new Dictionary<DateOnly, decimal>();
        foreach (var (date, hours) in persisted)
        {
            var weekStart = WeekStartOf(firstAttributedWeekStart, date);
            hoursByWeek[weekStart] = hoursByWeek.GetValueOrDefault(weekStart) + hours;
        }

        foreach (var (date, hours) in plannedHours)
        {
            if (date < firstAttributedWeekStart || date > queryEnd)
            {
                continue;
            }

            var weekStart = WeekStartOf(firstAttributedWeekStart, date);
            hoursByWeek[weekStart] = hoursByWeek.GetValueOrDefault(weekStart) + hours;
        }

        return hoursByWeek.Values.Sum(hours => Math.Max(0m, hours - threshold));
    }

    // Weeks tile in fixed 7-day steps from any known week start, so a single resolved anchor replaces a
    // per-day IWeekConfiguration round trip. Only valid for dates on or after the anchor.
    private static DateOnly WeekStartOf(DateOnly anchorWeekStart, DateOnly date)
    {
        var dayOffset = date.DayNumber - anchorWeekStart.DayNumber;
        return anchorWeekStart.AddDays((dayOffset / DaysPerWeek) * DaysPerWeek);
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

    private static ScheduleValidationNotificationDto BuildOvertimeCapEntry(
        Guid clientId,
        string clientName,
        DateOnly reportDate,
        PeriodCapRule rule,
        decimal overtimeHours,
        RuleEnforcementMode mode)
    {
        var isBlocked = mode == RuleEnforcementMode.Block;
        var commentParams = new Dictionary<string, string>
        {
            ["actualHours"] = overtimeHours.ToString("F1", CultureInfo.InvariantCulture),
            ["capHours"] = rule.CapHours.ToString("F0", CultureInfo.InvariantCulture),
        };

        // A trailing CustomWeeks window has no speaking period name, so it uses its own translation key
        // rendering the window length ("within the last N weeks") instead of the fixed-period key's
        // enum-named period - every parameter added here is displayed by its key's translation.
        string commentKey;
        if (rule.Period == PeriodCapPeriod.CustomWeeks)
        {
            commentKey = ScheduleValidationKeys.PeriodCapOvertimeWindow;
            commentParams["windowWeeks"] = rule.CustomPeriodWeeks!.Value.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            commentKey = ScheduleValidationKeys.PeriodCapOvertime;
            commentParams["period"] = rule.Period.ToString();
        }

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
            Comment = commentKey,
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
                        "PeriodCapRule with period CustomWeeks requires CustomPeriodWeeks to be set; this indicates a data integrity issue - the region-setup importer validates CustomPeriodWeeks on write.");
                }

                var windowDays = rule.CustomPeriodWeeks.Value * DaysPerWeek;
                return (date.AddDays(-(windowDays - 1)), date);

            default:
                throw new ArgumentOutOfRangeException(nameof(rule), rule.Period, "Unknown period cap period.");
        }
    }
}
