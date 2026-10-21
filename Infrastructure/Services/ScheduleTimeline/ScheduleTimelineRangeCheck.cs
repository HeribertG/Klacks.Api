// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Range check of the schedule timeline worker: rebuilds the timeline of every client with blocks in the period,
/// collects all findings and collisions and pushes both as a full refresh. Scoped and resolved from the worker's
/// per-attempt scope. Logs under the worker's category on purpose: appsettings.Development.json enables its Debug
/// traces only for that category.
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
using Klacks.Api.Infrastructure.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

public sealed class ScheduleTimelineRangeCheck
{
    /// <summary>
    /// A range check spans every visible client over the whole period, so the travel-time pairing has an
    /// upper bound on routing lookups. The routing service caches per address pair for 30 days, so the
    /// number of real API calls stays far below this; the cap only guards a cold cache on a huge period.
    /// </summary>
    private const int MaxTravelTimeLookupsPerRangeCheck = 2000;

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

    public ScheduleTimelineRangeCheck(
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

    public async Task RunAsync(DateOnly startDate, DateOnly endDate, Guid? analyseToken, CancellationToken cancellationToken)
    {
        var rows = await _loader.LoadRangeAsync(startDate, endDate, analyseToken, cancellationToken);

        var clientNameLookup = ScheduleClientNames.Build(rows.Works, rows.WorkChanges);
        var scheduleBlocks = _timelineCalculationService.CalculateScheduleBlocks(rows.Works, rows.WorkChanges, rows.Breaks);
        var spillIn = await _loader.LoadHolidaySpillInAsync(startDate, analyseToken, cancellationToken);

        var groupedByClient = scheduleBlocks.GroupBy(b => b.ClientId).ToList();
        var clientIds = groupedByClient.Select(g => g.Key).ToList();
        var policies = await _policyResolver.GetForClientsAsync(clientIds, startDate);
        var shiftAddressLookup = await _loader.LoadShiftAddressLookupAsync(rows.Works, cancellationToken);
        var travelApiKeyConfigured = await _travelTimeService.IsApiKeyConfiguredAsync();

        var run = new ScheduleTimelineRangeRun
        {
            StartDate = startDate,
            EndDate = endDate,
            AnalyseToken = analyseToken,
            ClientNames = clientNameLookup,
            Policies = policies,
            ShiftAddresses = shiftAddressLookup,
            TravelApiKeyConfigured = travelApiKeyConfigured,
            SpillIn = spillIn,
            OnCallBreakIds = rows.OnCallBreakIds,
            TravelLookupBudget = MaxTravelTimeLookupsPerRangeCheck,
        };

        foreach (var group in groupedByClient)
        {
            await CheckClientAsync(run, group, cancellationToken);
        }

        await AddPeriodWideEntriesAsync(run, rows.Works, clientIds, cancellationToken);

        _logger.LogDebug("[COLLISION-TRACE] RangeCheck results: {CollisionCount} collisions, {ValidationCount} validations, {ClientCount} clients checked, {WorkCount} works, {BreakCount} breaks",
            run.Collisions.Count, run.Entries.Count, groupedByClient.Count(), rows.Works.Count, rows.Breaks.Count);

        await PublishAsync(run);
    }

    private async Task CheckClientAsync(ScheduleTimelineRangeRun run, IGrouping<Guid, ScheduleBlock> group, CancellationToken cancellationToken)
    {
        var timeline = new ClientTimeline(group.Key);
        timeline.AddBlocks(group);
        timeline.SortBlocks();
        _timelineStore.SetTimeline(group.Key, timeline);

        run.ClientNames.TryGetValue(group.Key, out var clientName);
        clientName ??= string.Empty;

        var policy = run.Policies.TryGetValue(group.Key, out var found)
            ? found
            : await _policyResolver.GetForClientAsync(group.Key, run.StartDate);

        var entries = run.Entries;
        ScheduleValidationBuilder.AddRestViolations(entries, timeline, clientName, policy);
        ScheduleValidationBuilder.AddOvertime(entries, timeline, clientName, run.StartDate, run.EndDate, policy);
        ScheduleValidationBuilder.AddConsecutiveDays(entries, timeline, clientName, run.StartDate, run.EndDate, policy);
        ScheduleValidationBuilder.AddWeeklyOvertime(entries, timeline, clientName, run.StartDate, run.EndDate, policy);
        ScheduleValidationBuilder.AddMinRestDays(entries, timeline, clientName, run.StartDate, run.EndDate, policy);
        await _complianceRules.AddRangeEntriesAsync(entries, group.Key, clientName, run.StartDate, run.EndDate, run.AnalyseToken, cancellationToken);

        await AddTravelTimeEntriesAsync(run, timeline, clientName, cancellationToken);

        entries.AddRange(await _holidayWorkEvaluator.EvaluateAsync(group.Key, clientName, HolidayWorkTimelineCheck.RangeCandidates(timeline, run.StartDate, run.SpillIn), cancellationToken));

        ScheduleValidationBuilder.AddOnCallOverlaps(entries, timeline, clientName, run.OnCallBreakIds);
        run.Collisions.AddRange(TimelineCollisionNotificationBuilder.BuildList(timeline, run.ClientNames, run.OnCallBreakIds));
    }

    private async Task AddTravelTimeEntriesAsync(
        ScheduleTimelineRangeRun run, ClientTimeline timeline, string clientName, CancellationToken cancellationToken)
    {
        if (run.TravelLookupBudget <= 0)
        {
            run.ClientsWithoutTravelCheck++;
            return;
        }

        try
        {
            run.TravelLookupBudget -= await TimelineTravelTimeEntries.AddAsync(run.Entries, timeline, clientName, run.ShiftAddresses, _travelTimeService, run.TravelApiKeyConfigured, cancellationToken, run.TravelLookupBudget);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Travel time check failed for Client {ClientId} in range {Start} - {End}", timeline.ClientId, run.StartDate, run.EndDate);
        }
    }

    private async Task AddPeriodWideEntriesAsync(
        ScheduleTimelineRangeRun run, List<Work> works, List<Guid> clientIds, CancellationToken cancellationToken)
    {
        run.Entries.AddRange(await HolidayWorkTimelineCheck.EvaluateSpillInOnlyAsync(_holidayWorkEvaluator, run.SpillIn, clientIds, cancellationToken));

        if (run.ClientsWithoutTravelCheck > 0)
        {
            _logger.LogWarning(
                "Travel time budget of {Budget} lookups exhausted for range {Start} - {End}: {ClientCount} client(s) were not checked for travel time",
                MaxTravelTimeLookupsPerRangeCheck, run.StartDate, run.EndDate, run.ClientsWithoutTravelCheck);
        }

        await TimelineQualificationEntries.AddAsync(run.Entries, works, run.ClientNames, _eligibilityMatrixBuilder, cancellationToken);

        run.Entries.AddRange(await _planningRuleEvaluator.EvaluateRangeAsync(clientIds, run.StartDate, run.EndDate, run.AnalyseToken, run.ClientNames, cancellationToken));
    }

    private async Task PublishAsync(ScheduleTimelineRangeRun run)
    {
        var collisionNotification = new CollisionListNotificationDto
        {
            Collisions = run.Collisions,
            IsFullRefresh = true,
            AnalyseToken = run.AnalyseToken
        };
        await _notificationService.NotifyCollisionsDetected(collisionNotification);
        _logger.LogDebug("[COLLISION-TRACE] NotifyCollisionsDetected SENT ({Count} collisions)", run.Collisions.Count);

        await _escalationService.EscalateBlockedViolationsAsync(run.Entries);

        var validationNotification = new ScheduleValidationListNotificationDto
        {
            Entries = run.Entries,
            IsFullRefresh = true,
            AnalyseToken = run.AnalyseToken
        };
        await _notificationService.NotifyScheduleValidationsDetected(validationNotification);
        _logger.LogDebug("[COLLISION-TRACE] NotifyScheduleValidationsDetected SENT ({Count} entries)", run.Entries.Count);
    }
}
