// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Single (client/day) check of the schedule timeline worker: rebuilds the client's timeline for the edited day,
/// collects every finding for that day and pushes the collisions and the partial validation list to the clients.
/// Scoped and resolved from the worker's per-attempt scope. Logs under the worker's category on purpose:
/// appsettings.Development.json enables its Debug traces only for that category.
/// </summary>
/// <param name="timelineStore">Singleton cache of the latest timeline per client</param>
/// <param name="complianceRules">Configurable rule evaluators (period caps, rotation, counters, windows, compensatory rest)</param>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

public sealed class ScheduleTimelineSingleCheck
{
    /// <summary>
    /// A single check reports rest violations whose earlier shift is owned by the edited day or the day
    /// before; the client replaces exactly those dates on a single-day push. Loading two days on each side
    /// keeps every such pair visible even when DstAware moves a block's owner date across local midnight.
    /// </summary>
    private const int RestReportDaysBefore = 1;
    private const int RestWindowLoadDays = 2;

    private readonly ScheduleTimelineCheckLoader _loader;
    private readonly ITimelineCalculationService _timelineCalculationService;
    private readonly IScheduleTimelineStore _timelineStore;
    private readonly IWorkNotificationService _notificationService;
    private readonly ITravelTimeCalculationService _travelTimeService;
    private readonly ISchedulingPolicyResolver _policyResolver;
    private readonly IEligibilityMatrixBuilder _eligibilityMatrixBuilder;
    private readonly ScheduleTimelineComplianceRules _complianceRules;
    private readonly IComplianceEscalationService _escalationService;
    private readonly IHolidayWorkEvaluator _holidayWorkEvaluator;
    private readonly IPlanningRuleEvaluatorService _planningRuleEvaluator;
    private readonly ILogger<ScheduleTimelineBackgroundService> _logger;

    public ScheduleTimelineSingleCheck(
        ScheduleTimelineCheckLoader loader,
        ITimelineCalculationService timelineCalculationService,
        IScheduleTimelineStore timelineStore,
        IWorkNotificationService notificationService,
        ITravelTimeCalculationService travelTimeService,
        ISchedulingPolicyResolver policyResolver,
        IEligibilityMatrixBuilder eligibilityMatrixBuilder,
        ScheduleTimelineComplianceRules complianceRules,
        IComplianceEscalationService escalationService,
        IHolidayWorkEvaluator holidayWorkEvaluator,
        IPlanningRuleEvaluatorService planningRuleEvaluator,
        ILogger<ScheduleTimelineBackgroundService> logger)
    {
        _loader = loader;
        _timelineCalculationService = timelineCalculationService;
        _timelineStore = timelineStore;
        _notificationService = notificationService;
        _travelTimeService = travelTimeService;
        _policyResolver = policyResolver;
        _eligibilityMatrixBuilder = eligibilityMatrixBuilder;
        _complianceRules = complianceRules;
        _escalationService = escalationService;
        _holidayWorkEvaluator = holidayWorkEvaluator;
        _planningRuleEvaluator = planningRuleEvaluator;
        _logger = logger;
    }

    public async Task RunAsync(Guid clientId, DateOnly date, Guid? analyseToken, CancellationToken cancellationToken)
    {
        var rows = await _loader.LoadDayAsync(clientId, date, analyseToken, cancellationToken);

        var clientNameLookup = ScheduleClientNames.Build(rows.Works, rows.WorkChanges);
        var scheduleBlocks = _timelineCalculationService.CalculateScheduleBlocks(rows.Works, rows.WorkChanges, rows.Breaks);
        var clientBlocks = scheduleBlocks.Where(b => b.ClientId == clientId).ToList();

        var timeline = new ClientTimeline(clientId);
        timeline.AddBlocks(clientBlocks);
        timeline.SortBlocks();
        _timelineStore.SetTimeline(clientId, timeline);

        clientNameLookup.TryGetValue(clientId, out var clientName);
        clientName ??= string.Empty;

        var entries = await CollectEntriesAsync(rows, timeline, clientNameLookup, clientName, date, analyseToken, cancellationToken);

        var collisionNotification = TimelineCollisionNotificationBuilder.BuildNotification(timeline, clientNameLookup, false, clientId, date, analyseToken, rows.OnCallBreakIds);
        await _notificationService.NotifyCollisionsDetected(collisionNotification);

        await _escalationService.EscalateBlockedViolationsAsync(entries);

        var validationNotification = new ScheduleValidationListNotificationDto
        {
            Entries = entries,
            IsFullRefresh = false,
            CheckedClientId = clientId,
            CheckedDate = date,
            AnalyseToken = analyseToken
        };
        await _notificationService.NotifyScheduleValidationsDetected(validationNotification);
    }

    /// <summary>
    /// Collects the day's findings in their fixed order. Planning rules contribute only findings dated on the
    /// checked day: the partial refresh replaces exactly that client-day in the UI, so an entry anchored elsewhere
    /// would stay behind; the next range check reports those. Escalation to Error for rules configured as Block
    /// happens after collection: entries whose key the escalation map does not know - the evaluator group, which
    /// already resolves its own severity - pass through untouched, so handing over the whole list cannot
    /// double-escalate them.
    /// </summary>
    private async Task<List<ScheduleValidationNotificationDto>> CollectEntriesAsync(
        TimelineCheckRows rows,
        ClientTimeline timeline,
        Dictionary<Guid, string> clientNameLookup,
        string clientName,
        DateOnly date,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var clientId = timeline.ClientId;
        var entries = new List<ScheduleValidationNotificationDto>();

        var policy = await _policyResolver.GetForClientAsync(clientId, date);
        var windowTimeline = await _loader.LoadClientTimelineAsync(
            clientId, date.AddDays(-RestWindowLoadDays), date.AddDays(RestWindowLoadDays), analyseToken, cancellationToken);
        ScheduleValidationBuilder.AddRestViolations(entries, windowTimeline, clientName, policy, date.AddDays(-RestReportDaysBefore), date);
        ScheduleValidationBuilder.AddOvertime(entries, windowTimeline, clientName, date, date, policy);
        await TimelineQualificationEntries.AddAsync(entries, rows.OwnWorks, clientNameLookup, _eligibilityMatrixBuilder, cancellationToken);
        await _complianceRules.AddDayEntriesAsync(entries, clientId, clientName, date, analyseToken, cancellationToken);

        await AddWeekAwareEntriesAsync(entries, clientId, clientName, date, analyseToken, policy, cancellationToken);

        if (HolidayWorkTimelineCheck.WorksOnDate(windowTimeline, date))
        {
            entries.AddRange(await _holidayWorkEvaluator.EvaluateAsync(clientId, clientName, [date], cancellationToken));
        }

        entries.AddRange(await _planningRuleEvaluator.EvaluateDayAsync(clientId, clientName, date, analyseToken, cancellationToken));

        await AddTravelTimeEntriesAsync(entries, timeline, rows.Works, clientName, date, cancellationToken);

        ScheduleValidationBuilder.AddOnCallOverlaps(entries, timeline, clientName, rows.OnCallBreakIds);
        return entries;
    }

    /// <summary>
    /// Runs the checks that need more than the edited day. The surrounding single check loads the edited
    /// day (plus a few days around it for rest and daily overtime only), which is too narrow for a weekly
    /// cap, a weekly rest-day quota or a run of consecutive days, so this loads the full ISO week separately.
    /// All three report inside the edited day's ISO week, which is the window the client retracts when
    /// that week is re-evaluated: the two weekly checks anchor on the week's Monday exactly like the
    /// range check, the consecutive-day check anchors on the day its run starts. GetConsecutiveWorkDays
    /// counts forward, so a run reaching into the week from before Monday is under-counted here — never
    /// over-counted — and the range check, which sees the whole loaded period, reports it in full.
    /// Breaks are loaded over the same window: a break day counts as a rest day, so a narrower break load
    /// would make AddMinRestDays under-count rest days and report false positives.
    /// </summary>
    /// <param name="date">The edited day; its ISO week defines the load window</param>
    /// <param name="policy">Supplies MaxWeeklyHours, MinRestDays and MaxConsecutiveDays for the client</param>
    private async Task AddWeekAwareEntriesAsync(
        List<ScheduleValidationNotificationDto> entries,
        Guid clientId,
        string clientName,
        DateOnly date,
        Guid? analyseToken,
        SchedulingPolicy policy,
        CancellationToken cancellationToken)
    {
        var (weekStart, weekEnd) = ScheduleValidationBuilder.IsoWeekOf(date);

        var weekTimeline = await _loader.LoadClientTimelineAsync(clientId, weekStart, weekEnd, analyseToken, cancellationToken);

        ScheduleValidationBuilder.AddWeeklyOvertime(entries, weekTimeline, clientName, weekStart, weekEnd, policy);
        ScheduleValidationBuilder.AddMinRestDays(entries, weekTimeline, clientName, weekStart, weekEnd, policy);
        ScheduleValidationBuilder.AddConsecutiveDays(entries, weekTimeline, clientName, weekStart, weekEnd, policy);
    }

    private async Task AddTravelTimeEntriesAsync(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        List<Work> works,
        string clientName,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        try
        {
            var shiftAddressLookup = TimelineTravelTimeEntries.BuildShiftAddressLookup(works);
            var apiKeyConfigured = await _travelTimeService.IsApiKeyConfiguredAsync();
            await TimelineTravelTimeEntries.AddAsync(entries, timeline, clientName, shiftAddressLookup, _travelTimeService, apiKeyConfigured, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Travel time check failed for Client {ClientId} on {Date}", timeline.ClientId, date);
        }
    }
}
