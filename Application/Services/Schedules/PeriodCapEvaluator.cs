// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
/// RollingAverage). Both CapHours-based modes additionally emit an approaching-cap Warning once the
/// evaluated hours reach WarnAtPercent percent of CapHours while still at or under the cap itself;
/// this pre-warning is informational only and never escalates to Error regardless of enforcement mode.
/// The evaluation for several clients runs the same rule evaluation per client; it reads the rule list once,
/// each enforcement mode at most once, and the contract data, employment starts and TotalHours / rolling-average
/// baseline hours of all clients in grouped reads (one per distinct day or window) before the per-client pass; the
/// read-ahead and the evaluation take their windows from the same <see cref="PeriodCapWindows"/> methods, and a value
/// that was not read ahead throws instead of being read for the client alone. The OvertimeHours scope is still read
/// per client. The window arithmetic lives in <see cref="PeriodCapWindows"/>.
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
using Klacks.Api.Application.DTOs.Schedules;
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
    private const decimal PercentDivisor = 100m;

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
        var reads = new CapReads(
            _enforcementResolver.GetModeAsync,
            () => _membershipStartResolver.GetValidFromAsync(clientId),
            (start, end) => ResolveCapBaselineHoursAsync(clientId, start, end, analyseToken));

        return await EvaluateRulesAsync(
            clientId, clientName, plannedHours, rules, latestCandidateDate, reads, analyseToken, cancellationToken);
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedForClientsAsync(
        IReadOnlyList<ClientPlannedHours> clients,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        var planned = clients
            .Where(c => c.PlannedHours.Count > 0)
            .Select(c => (Client: c, LatestCandidateDate: c.PlannedHours.Max(p => p.Date)))
            .ToList();
        if (planned.Count == 0)
        {
            return [];
        }

        var allRules = await _ruleRepository.GetAllActiveAsync();
        var contracts = allRules.All(r => r.SchedulingRuleId == null)
            ? null
            : await ClientContractDataBatch.LoadAsync(
                _contractDataProvider, planned.Select(p => (p.Client.ClientId, p.LatestCandidateDate)));
        var evaluations = planned
            .Select(p => (
                p.Client,
                p.LatestCandidateDate,
                Rules: contracts == null
                    ? allRules
                    : ApplicableRules(allRules, contracts[(p.Client.ClientId, p.LatestCandidateDate)].SchedulingRuleId)))
            .ToList();

        var modes = new Dictionary<string, RuleEnforcementMode>();
        var membershipStarts = await _membershipStartResolver.GetValidFromForClientsAsync(
            evaluations.Where(e => e.Rules.Any(PeriodCapWindows.IsRollingAverageRule)).Select(e => e.Client.ClientId).Distinct().ToList(),
            cancellationToken);
        var baselines = await LoadBaselineHoursAsync(
            evaluations.SelectMany(e => PeriodCapWindows.BaselineWindows(e.Client.PlannedHours, e.Rules, membershipStarts.GetValueOrDefault(e.Client.ClientId))
                .Select(window => (e.Client.ClientId, window.Start, window.End))),
            analyseToken);

        var entries = new List<ScheduleValidationNotificationDto>();
        foreach (var (client, latestCandidateDate, rules) in evaluations)
        {
            var clientId = client.ClientId;
            var reads = new CapReads(
                async ruleName =>
                {
                    if (!modes.TryGetValue(ruleName, out var mode))
                    {
                        mode = await _enforcementResolver.GetModeAsync(ruleName);
                        modes[ruleName] = mode;
                    }

                    return mode;
                },
                () => Task.FromResult(membershipStarts[clientId]),
                (start, end) => Task.FromResult(baselines[(clientId, start, end)]));

            entries.AddRange(await EvaluateRulesAsync(
                clientId, string.Empty, client.PlannedHours, rules, latestCandidateDate, reads, analyseToken, cancellationToken));
        }

        return entries;
    }

    private sealed record CapReads(
        Func<string, Task<RuleEnforcementMode>> Mode,
        Func<Task<DateOnly?>> MembershipStart,
        Func<DateOnly, DateOnly, Task<decimal>> BaselineHours);

    private async Task<List<ScheduleValidationNotificationDto>> EvaluateRulesAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        List<PeriodCapRule> rules,
        DateOnly latestCandidateDate,
        CapReads reads,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var rollingAverageRules = rules.Where(PeriodCapWindows.IsRollingAverageRule).ToList();
        var fixedPeriodRules = rules.Where(r => !PeriodCapWindows.IsRollingAverageRule(r) && r.Scope == PeriodCapScope.TotalHours).ToList();
        var overtimeCapRules = rules.Where(r => !PeriodCapWindows.IsRollingAverageRule(r) && r.Scope == PeriodCapScope.OvertimeHours).ToList();

        if (fixedPeriodRules.Count == 0 && rollingAverageRules.Count == 0 && overtimeCapRules.Count == 0)
        {
            return [];
        }

        var entries = new List<ScheduleValidationNotificationDto>();

        if (fixedPeriodRules.Count > 0 || overtimeCapRules.Count > 0)
        {
            var mode = await reads.Mode(ComplianceRuleNames.PeriodCap);
            if (fixedPeriodRules.Count > 0)
            {
                entries.AddRange(await EvaluateFixedPeriodRulesAsync(
                    clientId, clientName, plannedHours, fixedPeriodRules, mode, reads));
            }

            if (overtimeCapRules.Count > 0)
            {
                entries.AddRange(await EvaluateOvertimeCapRulesAsync(
                    clientId, clientName, plannedHours, overtimeCapRules, latestCandidateDate, mode, analyseToken, cancellationToken));
            }
        }

        if (rollingAverageRules.Count > 0)
        {
            var mode = await reads.Mode(ComplianceRuleNames.RollingAverage);
            var membershipStart = await reads.MembershipStart();
            entries.AddRange(await EvaluateRollingAverageRulesAsync(
                clientId, clientName, plannedHours, rollingAverageRules, membershipStart, mode, reads));
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
        return ApplicableRules(rules, effectiveData.SchedulingRuleId);
    }

    private static List<PeriodCapRule> ApplicableRules(List<PeriodCapRule> rules, Guid? schedulingRuleId) =>
        rules
            .Where(r => r.SchedulingRuleId == null || r.SchedulingRuleId == schedulingRuleId)
            .ToList();

    /// <summary>
    /// Reads the persisted period hours of the requested client windows with one call per distinct window.
    /// </summary>
    /// <param name="requests">The (client, window start, window end) triples to read</param>
    /// <param name="analyseToken">Scenario token; null reads the real schedule</param>
    private async Task<Dictionary<(Guid ClientId, DateOnly Start, DateOnly End), decimal>> LoadBaselineHoursAsync(
        IEnumerable<(Guid ClientId, DateOnly Start, DateOnly End)> requests,
        Guid? analyseToken)
    {
        var baselines = new Dictionary<(Guid ClientId, DateOnly Start, DateOnly End), decimal>();

        foreach (var window in requests.Distinct().GroupBy(r => (r.Start, r.End), r => r.ClientId))
        {
            var hoursByClient = await _periodHoursService.CalculateHoursForClientsAsync(
                window.ToList(), window.Key.Start, window.Key.End, analyseToken);
            foreach (var (clientId, hours) in hoursByClient)
            {
                baselines[(clientId, window.Key.Start, window.Key.End)] = hours;
            }
        }

        return baselines;
    }

    private async Task<decimal> ResolveCapBaselineHoursAsync(
        Guid clientId, DateOnly start, DateOnly end, Guid? analyseToken)
    {
        var baseline = await _periodHoursService.CalculatePeriodHoursAsync(clientId, start, end, analyseToken);
        return baseline.Hours;
    }

    private async Task<List<ScheduleValidationNotificationDto>> EvaluateFixedPeriodRulesAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        List<PeriodCapRule> fixedPeriodRules,
        RuleEnforcementMode mode,
        CapReads reads)
    {
        var entries = new List<ScheduleValidationNotificationDto>();

        foreach (var rule in fixedPeriodRules)
        {
            foreach (var group in PeriodCapWindows.FixedPeriodWindows(rule, plannedHours))
            {
                var (start, end) = group.Key;
                var additionalHours = group.Sum(x => x.Hours);
                var baselineHours = await reads.BaselineHours(start, end);
                var projectedHours = baselineHours + additionalHours;
                var isOverCap = projectedHours > rule.CapHours;
                if (!isOverCap && !ReachesWarnThreshold(rule, projectedHours))
                {
                    continue;
                }

                var reportDate = group.Min(x => x.Date);
                entries.Add(isOverCap
                    ? BuildFixedPeriodEntry(clientId, clientName, reportDate, rule, projectedHours, mode)
                    : BuildFixedPeriodApproachingEntry(clientId, clientName, reportDate, rule, projectedHours));
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
                .Select(p => (Window: PeriodCapWindows.ResolveFixedPeriodBoundaries(rule, p.Date), p.Date))
                .GroupBy(x => x.Window);

            foreach (var group in windowGroups)
            {
                var (start, end) = group.Key;
                var overtimeHours = basis == OvertimeBasis.Week
                    ? await SumWeeklyOvertimeAsync(clientId, plannedHours, start, end, threshold, analyseToken, cancellationToken)
                    : await SumDailyOvertimeAsync(clientId, plannedHours, start, end, threshold, analyseToken);
                var isOverCap = overtimeHours > rule.CapHours;
                if (!isOverCap && !ReachesWarnThreshold(rule, overtimeHours))
                {
                    continue;
                }

                var reportDate = group.Min(x => x.Date);
                entries.Add(isOverCap
                    ? BuildOvertimeCapEntry(clientId, clientName, reportDate, rule, overtimeHours, mode)
                    : BuildOvertimeCapApproachingEntry(clientId, clientName, reportDate, rule, overtimeHours));
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
            : weekStartOfWindowStart.AddDays(CalendarConstants.DaysPerWeek);
        if (firstAttributedWeekStart > windowEnd)
        {
            return 0m;
        }

        var lastAttributedWeekStart = WeekStartOf(firstAttributedWeekStart, windowEnd);
        var queryEnd = lastAttributedWeekStart.AddDays(CalendarConstants.DaysPerWeek - 1);

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
        return anchorWeekStart.AddDays((dayOffset / CalendarConstants.DaysPerWeek) * CalendarConstants.DaysPerWeek);
    }

    private async Task<List<ScheduleValidationNotificationDto>> EvaluateRollingAverageRulesAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        List<PeriodCapRule> rollingAverageRules,
        DateOnly? membershipStart,
        RuleEnforcementMode mode,
        CapReads reads)
    {
        var entries = new List<ScheduleValidationNotificationDto>();

        foreach (var rule in rollingAverageRules)
        {
            foreach (var (date, windowStart, effectiveWeeks) in PeriodCapWindows.RollingAverageWindows(rule, plannedHours, membershipStart))
            {
                var additionalHours = plannedHours
                    .Where(p => p.Date >= windowStart && p.Date <= date)
                    .Sum(p => p.Hours);

                var baselineHours = await reads.BaselineHours(windowStart, date);
                var projectedAverage = (baselineHours + additionalHours) / effectiveWeeks;
                if (projectedAverage <= rule.MaxAverageWeeklyHours!.Value)
                {
                    continue;
                }

                entries.Add(BuildRollingAverageEntry(clientId, clientName, date, rule, projectedAverage, mode));
            }
        }

        return entries;
    }

    // The approaching-cap pre-warning is informational only: it fires between WarnAtPercent percent of
    // CapHours and the cap itself and stays a Warning even under Block enforcement - only an actual cap
    // breach may block.
    private static bool ReachesWarnThreshold(PeriodCapRule rule, decimal actualHours)
    {
        if (!rule.WarnAtPercent.HasValue || rule.CapHours <= 0)
        {
            return false;
        }

        var warnThresholdHours = rule.CapHours * rule.WarnAtPercent.Value / PercentDivisor;
        return actualHours >= warnThresholdHours;
    }

    private static ScheduleValidationNotificationDto BuildFixedPeriodApproachingEntry(
        Guid clientId,
        string clientName,
        DateOnly reportDate,
        PeriodCapRule rule,
        decimal projectedHours)
    {
        return new ScheduleValidationNotificationDto
        {
            Type = ScheduleValidationType.Warning,
            ClientId = clientId,
            ClientName = clientName,
            Date = reportDate,
            Comment = ScheduleValidationKeys.PeriodCapApproaching,
            CommentParams = new Dictionary<string, string>
            {
                ["actualHours"] = projectedHours.ToString("F1", CultureInfo.InvariantCulture),
                ["capHours"] = rule.CapHours.ToString("F0", CultureInfo.InvariantCulture),
                ["warnAtPercent"] = rule.WarnAtPercent!.Value.ToString(CultureInfo.InvariantCulture),
                ["period"] = rule.Period.ToString(),
            },
        };
    }

    private static ScheduleValidationNotificationDto BuildOvertimeCapApproachingEntry(
        Guid clientId,
        string clientName,
        DateOnly reportDate,
        PeriodCapRule rule,
        decimal overtimeHours)
    {
        var commentParams = new Dictionary<string, string>
        {
            ["actualHours"] = overtimeHours.ToString("F1", CultureInfo.InvariantCulture),
            ["capHours"] = rule.CapHours.ToString("F0", CultureInfo.InvariantCulture),
            ["warnAtPercent"] = rule.WarnAtPercent!.Value.ToString(CultureInfo.InvariantCulture),
        };

        string commentKey;
        if (rule.Period == PeriodCapPeriod.CustomWeeks)
        {
            commentKey = ScheduleValidationKeys.PeriodCapOvertimeWindowApproaching;
            commentParams["windowWeeks"] = rule.CustomPeriodWeeks!.Value.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            commentKey = ScheduleValidationKeys.PeriodCapOvertimeApproaching;
            commentParams["period"] = rule.Period.ToString();
        }

        return new ScheduleValidationNotificationDto
        {
            Type = ScheduleValidationType.Warning,
            ClientId = clientId,
            ClientName = clientName,
            Date = reportDate,
            Comment = commentKey,
            CommentParams = commentParams,
        };
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
}
