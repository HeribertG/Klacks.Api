// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background worker for schedule validation: collisions, rest periods, working hours, travel times.
/// Receives check requests via Channel and sends results via SignalR.
/// </summary>
using System.Net.Sockets;
using System.Threading.Channels;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Infrastructure.Services;

public class ScheduleTimelineBackgroundService : BackgroundService, IScheduleTimelineService
{
    private const double TravelTimeWarningFactor = 1.5;

    // A range check spans every visible client over the whole period, so the travel-time pairing has an
    // upper bound on routing lookups. The routing service caches per address pair for 30 days, so the
    // number of real API calls stays far below this; the cap only guards a cold cache on a huge period.
    private const int MaxTravelTimeLookupsPerRangeCheck = 2000;

    private const int MaxTransientRetries = 1;
    private static readonly TimeSpan TransientRetryBaseDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan TransientRetryJitter = TimeSpan.FromMilliseconds(250);
    private static readonly HashSet<string> TransientPostgresSqlStates = new(StringComparer.Ordinal)
    {
        "40001", // serialization_failure
        "40P01", // deadlock_detected
        "08000", // connection_exception
        "08003", // connection_does_not_exist
        "08006", // connection_failure
        "08004", // sqlclient_unable_to_establish_sqlconnection
        "57P03", // cannot_connect_now
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduleTimelineBackgroundService> _logger;
    private readonly IScheduleTimelineStore _timelineStore;
    private readonly Channel<TimelineCheckRequest> _channel;

    public ScheduleTimelineBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ScheduleTimelineBackgroundService> logger,
        IScheduleTimelineStore timelineStore)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timelineStore = timelineStore;

        _channel = Channel.CreateBounded<TimelineCheckRequest>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
    }

    public void QueueCheck(Guid clientId, DateOnly date, Guid? analyseToken)
    {
        _logger.LogDebug("[COLLISION-TRACE] QueueCheck queued Client={ClientId} Date={Date} token={Token}",
            clientId, date, analyseToken?.ToString() ?? "null");

        var request = new TimelineCheckRequest
        {
            ClientId = clientId,
            Date = date,
            AnalyseToken = analyseToken
        };

        if (!_channel.Writer.TryWrite(request))
        {
            _logger.LogWarning(
                "Failed to queue timeline check for Client {ClientId} on {Date} token={Token}",
                clientId, date, analyseToken?.ToString() ?? "null");
        }
    }

    public void QueueRangeCheck(DateOnly startDate, DateOnly endDate, Guid? analyseToken)
    {
        _logger.LogDebug("[COLLISION-TRACE] QueueRangeCheck queued {Start} - {End} token={Token}",
            startDate, endDate, analyseToken?.ToString() ?? "null");

        var request = new TimelineCheckRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            IsRangeCheck = true,
            AnalyseToken = analyseToken
        };

        if (!_channel.Writer.TryWrite(request))
        {
            _logger.LogWarning(
                "Failed to queue timeline range check for {StartDate} to {EndDate} token={Token}",
                startDate, endDate, analyseToken?.ToString() ?? "null");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduleTimelineBackgroundService started");

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                // Drain the channel and deduplicate by semantic key. Different semantic keys
                // (e.g. SingleCheck Client A/Date X and RangeCheck 1.-14. April) MUST both run;
                // only requests that share the exact same key collapse (latest wins).
                var pending = new Dictionary<string, TimelineCheckRequest>(StringComparer.Ordinal)
                {
                    [BuildRequestKey(request)] = request
                };

                var totalRead = 1;
                while (_channel.Reader.TryRead(out var newer))
                {
                    pending[BuildRequestKey(newer)] = newer;
                    totalRead++;
                }

                var deduped = totalRead - pending.Count;
                if (deduped > 0)
                {
                    _logger.LogDebug("[COLLISION-TRACE] Drained {Total} requests, {Deduped} deduplicated, {Unique} unique to process",
                        totalRead, deduped, pending.Count);
                }

                foreach (var job in pending.Values)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    await ProcessJobWithRetryAsync(job, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("ScheduleTimelineBackgroundService stopped");
    }

    private static string BuildRequestKey(TimelineCheckRequest r)
        => r.IsRangeCheck
            ? $"range:{r.StartDate:O}:{r.EndDate:O}:{(r.AnalyseToken?.ToString() ?? "null")}"
            : $"single:{r.ClientId}:{r.Date:O}:{(r.AnalyseToken?.ToString() ?? "null")}";

    private async Task ProcessJobWithRetryAsync(TimelineCheckRequest job, CancellationToken stoppingToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<IWorkNotificationService>();
                var timelineCalculationService = scope.ServiceProvider.GetRequiredService<ITimelineCalculationService>();
                var travelTimeService = scope.ServiceProvider.GetRequiredService<ITravelTimeCalculationService>();
                var policyResolver = scope.ServiceProvider.GetRequiredService<ISchedulingPolicyResolver>();
                var eligibilityMatrixBuilder = scope.ServiceProvider.GetRequiredService<IEligibilityMatrixBuilder>();
                var periodCapEvaluator = scope.ServiceProvider.GetRequiredService<IPeriodCapEvaluator>();
                var restDayRotationEvaluator = scope.ServiceProvider.GetRequiredService<IRestDayRotationEvaluator>();
                var counterRuleEvaluator = scope.ServiceProvider.GetRequiredService<ICounterRuleEvaluator>();
                var restrictedTimeWindowEvaluator = scope.ServiceProvider.GetRequiredService<IRestrictedTimeWindowEvaluator>();
                var compensatoryRestReconciler = scope.ServiceProvider.GetRequiredService<ICompensatoryRestObligationReconciler>();
                var compensatoryRestEvaluator = scope.ServiceProvider.GetRequiredService<ICompensatoryRestEvaluator>();
                var escalationService = scope.ServiceProvider.GetRequiredService<IComplianceEscalationService>();

                if (job.IsRangeCheck)
                {
                    _logger.LogDebug("[COLLISION-TRACE] START RangeCheck {Start} - {End} token={Token}",
                        job.StartDate, job.EndDate, job.AnalyseToken?.ToString() ?? "null");
                    await ProcessRangeCheckAsync(dbContext, notificationService, timelineCalculationService, travelTimeService, policyResolver, eligibilityMatrixBuilder, periodCapEvaluator, restDayRotationEvaluator, counterRuleEvaluator, restrictedTimeWindowEvaluator, compensatoryRestReconciler, compensatoryRestEvaluator, escalationService, job.StartDate, job.EndDate, job.AnalyseToken, stoppingToken);
                    _logger.LogDebug("[COLLISION-TRACE] DONE RangeCheck {Start} - {End} token={Token}",
                        job.StartDate, job.EndDate, job.AnalyseToken?.ToString() ?? "null");
                }
                else
                {
                    _logger.LogDebug("[COLLISION-TRACE] START SingleCheck Client={ClientId} Date={Date} token={Token}",
                        job.ClientId, job.Date, job.AnalyseToken?.ToString() ?? "null");
                    await ProcessSingleCheckAsync(dbContext, notificationService, timelineCalculationService, travelTimeService, policyResolver, eligibilityMatrixBuilder, periodCapEvaluator, restDayRotationEvaluator, counterRuleEvaluator, restrictedTimeWindowEvaluator, compensatoryRestReconciler, compensatoryRestEvaluator, escalationService, job.ClientId, job.Date, job.AnalyseToken, stoppingToken);
                    _logger.LogDebug("[COLLISION-TRACE] DONE SingleCheck Client={ClientId} token={Token}",
                        job.ClientId, job.AnalyseToken?.ToString() ?? "null");
                }

                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxTransientRetries && IsTransientException(ex))
            {
                attempt++;
                var delay = TransientRetryBaseDelay + TimeSpan.FromMilliseconds(Random.Shared.Next(0, (int)TransientRetryJitter.TotalMilliseconds));
                _logger.LogWarning(ex,
                    "[COLLISION-TRACE] Transient failure on {Key} (attempt {Attempt}/{Max}), retrying in {Delay}ms",
                    BuildRequestKey(job), attempt, MaxTransientRetries, (int)delay.TotalMilliseconds);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                if (job.IsRangeCheck)
                {
                    _logger.LogError(ex, "[COLLISION-TRACE] FAILED RangeCheck {Start} - {End} token={Token} after {Attempts} attempt(s): {Message}",
                        job.StartDate, job.EndDate, job.AnalyseToken?.ToString() ?? "null", attempt + 1, ex.Message);
                }
                else
                {
                    _logger.LogError(ex, "[COLLISION-TRACE] FAILED SingleCheck Client={ClientId} Date={Date} token={Token} after {Attempts} attempt(s): {Message}",
                        job.ClientId, job.Date, job.AnalyseToken?.ToString() ?? "null", attempt + 1, ex.Message);
                }
                return;
            }
        }
    }

    private static bool IsTransientException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case TimeoutException:
                case DbUpdateConcurrencyException:
                case SocketException:
                    return true;
                case PostgresException pg when TransientPostgresSqlStates.Contains(pg.SqlState):
                    return true;
                case NpgsqlException:
                    return true;
            }
        }
        return false;
    }

    private async Task ProcessSingleCheckAsync(
        DataBaseContext dbContext,
        IWorkNotificationService notificationService,
        ITimelineCalculationService timelineCalculationService,
        ITravelTimeCalculationService travelTimeService,
        ISchedulingPolicyResolver policyResolver,
        IEligibilityMatrixBuilder eligibilityMatrixBuilder,
        IPeriodCapEvaluator periodCapEvaluator,
        IRestDayRotationEvaluator restDayRotationEvaluator,
        ICounterRuleEvaluator counterRuleEvaluator,
        IRestrictedTimeWindowEvaluator restrictedTimeWindowEvaluator,
        ICompensatoryRestObligationReconciler compensatoryRestReconciler,
        ICompensatoryRestEvaluator compensatoryRestEvaluator,
        IComplianceEscalationService escalationService,
        Guid clientId,
        DateOnly date,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var ownWorks = await dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Include(w => w.Shift).ThenInclude(s => s!.Client).ThenInclude(c => c!.Addresses)
            .Where(w => w.ClientId == clientId && w.CurrentDate == date && !w.IsDeleted && w.ParentWorkId == null && w.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var worksWithReplacementForClient = await dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Include(w => w.Shift).ThenInclude(s => s!.Client).ThenInclude(c => c!.Addresses)
            .Where(w => w.CurrentDate == date && !w.IsDeleted && w.ParentWorkId == null && w.AnalyseToken == analyseToken &&
                        dbContext.WorkChange.Any(wc =>
                            wc.WorkId == w.Id && !wc.IsDeleted && wc.ReplaceClientId == clientId))
            .ToListAsync(cancellationToken);

        var allWorks = ownWorks
            .Union(worksWithReplacementForClient, new WorkIdComparer())
            .ToList();

        var workIds = allWorks.Select(w => w.Id).ToList();
        var workChanges = workIds.Count > 0
            ? await dbContext.WorkChange
                .AsNoTracking()
                .Include(wc => wc.ReplaceClient)
                .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
                .ToListAsync(cancellationToken)
            : [];

        var breaks = await dbContext.Break
            .AsNoTracking()
            .Where(b => b.ClientId == clientId && b.CurrentDate == date && !b.IsDeleted && b.ParentWorkId == null && b.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var clientNameLookup = BuildClientNameLookup(allWorks, workChanges);
        var scheduleBlocks = timelineCalculationService.CalculateScheduleBlocks(allWorks, workChanges, breaks);
        var clientBlocks = scheduleBlocks.Where(b => b.ClientId == clientId).ToList();

        var timeline = new ClientTimeline(clientId);
        timeline.AddBlocks(clientBlocks);
        timeline.SortBlocks();
        _timelineStore.SetTimeline(clientId, timeline);

        var entries = new List<ScheduleValidationNotificationDto>();
        clientNameLookup.TryGetValue(clientId, out var clientName);
        clientName ??= string.Empty;

        var policy = await policyResolver.GetForClientAsync(clientId, date);
        ScheduleValidationBuilder.AddRestViolations(entries, timeline, clientName, policy);
        ScheduleValidationBuilder.AddOvertime(entries, timeline, clientName, date, date, policy);
        await AddQualificationEntriesAsync(entries, ownWorks, clientNameLookup, eligibilityMatrixBuilder, cancellationToken);
        entries.AddRange(await periodCapEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));
        entries.AddRange(await restDayRotationEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));
        entries.AddRange(await counterRuleEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));
        entries.AddRange(await restrictedTimeWindowEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));

        // Reconcile compensatory-rest obligations for the boundary window around the edited date, then
        // surface the open ones. Reconcile is real-mode only (the reconciler no-ops on a non-null token).
        await compensatoryRestReconciler.ReconcileAsync(clientId, date.AddDays(-ScenarioConstants.BoundaryDays), date.AddDays(ScenarioConstants.BoundaryDays), analyseToken, cancellationToken);
        entries.AddRange(await compensatoryRestEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));

        await AddWeekAwareEntriesAsync(dbContext, timelineCalculationService, entries, clientId, clientName, date, analyseToken, policy, cancellationToken);

        try
        {
            var shiftAddressLookup = BuildShiftAddressLookup(allWorks);
            var apiKeyConfigured = await travelTimeService.IsApiKeyConfiguredAsync();
            await AddTravelTimeEntriesAsync(entries, timeline, clientName, shiftAddressLookup, travelTimeService, apiKeyConfigured, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Travel time check failed for Client {ClientId} on {Date}", clientId, date);
        }

        var collisionNotification = BuildLegacyCollisionNotification(timeline, clientNameLookup, false, clientId, date, analyseToken);
        await notificationService.NotifyCollisionsDetected(collisionNotification);

        // Escalates Warning to Error for rules configured as Block. Entries whose key the escalation map
        // does not know — the evaluator group, which already resolves its own severity — pass through
        // untouched, so handing over the whole list cannot double-escalate them.
        await escalationService.EscalateBlockedViolationsAsync(entries);

        var validationNotification = new ScheduleValidationListNotificationDto
        {
            Entries = entries,
            IsFullRefresh = false,
            CheckedClientId = clientId,
            CheckedDate = date,
            AnalyseToken = analyseToken
        };
        await notificationService.NotifyScheduleValidationsDetected(validationNotification);
    }

    private async Task ProcessRangeCheckAsync(
        DataBaseContext dbContext,
        IWorkNotificationService notificationService,
        ITimelineCalculationService timelineCalculationService,
        ITravelTimeCalculationService travelTimeService,
        ISchedulingPolicyResolver policyResolver,
        IEligibilityMatrixBuilder eligibilityMatrixBuilder,
        IPeriodCapEvaluator periodCapEvaluator,
        IRestDayRotationEvaluator restDayRotationEvaluator,
        ICounterRuleEvaluator counterRuleEvaluator,
        IRestrictedTimeWindowEvaluator restrictedTimeWindowEvaluator,
        ICompensatoryRestObligationReconciler compensatoryRestReconciler,
        ICompensatoryRestEvaluator compensatoryRestEvaluator,
        IComplianceEscalationService escalationService,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var works = await dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Where(w => w.CurrentDate >= startDate && w.CurrentDate <= endDate && !w.IsDeleted && w.ParentWorkId == null && w.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var workIds = works.Select(w => w.Id).ToList();
        var workChanges = workIds.Count > 0
            ? await dbContext.WorkChange
                .AsNoTracking()
                .Include(wc => wc.ReplaceClient)
                .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
                .ToListAsync(cancellationToken)
            : [];

        var breaks = await dbContext.Break
            .AsNoTracking()
            .Where(b => b.CurrentDate >= startDate && b.CurrentDate <= endDate && !b.IsDeleted && b.ParentWorkId == null && b.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var clientNameLookup = BuildClientNameLookup(works, workChanges);
        var scheduleBlocks = timelineCalculationService.CalculateScheduleBlocks(works, workChanges, breaks);

        var groupedByClient = scheduleBlocks.GroupBy(b => b.ClientId).ToList();
        var allEntries = new List<ScheduleValidationNotificationDto>();
        var allCollisions = new List<CollisionNotificationDto>();

        var clientIds = groupedByClient.Select(g => g.Key).ToList();
        var policies = await policyResolver.GetForClientsAsync(clientIds, startDate);

        var shiftAddressLookup = await LoadShiftAddressLookupAsync(dbContext, works, cancellationToken);
        var travelApiKeyConfigured = await travelTimeService.IsApiKeyConfiguredAsync();
        var travelLookupBudget = MaxTravelTimeLookupsPerRangeCheck;
        var clientsWithoutTravelCheck = 0;

        foreach (var group in groupedByClient)
        {
            var timeline = new ClientTimeline(group.Key);
            timeline.AddBlocks(group);
            timeline.SortBlocks();
            _timelineStore.SetTimeline(group.Key, timeline);

            clientNameLookup.TryGetValue(group.Key, out var clientName);
            clientName ??= string.Empty;

            var policy = policies.TryGetValue(group.Key, out var found)
                ? found
                : await policyResolver.GetForClientAsync(group.Key, startDate);

            ScheduleValidationBuilder.AddRestViolations(allEntries, timeline, clientName, policy);
            ScheduleValidationBuilder.AddOvertime(allEntries, timeline, clientName, startDate, endDate, policy);
            ScheduleValidationBuilder.AddConsecutiveDays(allEntries, timeline, clientName, startDate, endDate, policy);
            ScheduleValidationBuilder.AddWeeklyOvertime(allEntries, timeline, clientName, startDate, endDate, policy);
            ScheduleValidationBuilder.AddMinRestDays(allEntries, timeline, clientName, startDate, endDate, policy);
            // Anchor choice differs on purpose: the period-cap evaluator resolves calendar windows from
            // its anchor and startDate keeps the historical behaviour; the trailing-window/counter
            // evaluators anchor at endDate so the latest changes in the range fall inside the inspected
            // window/period.
            allEntries.AddRange(await periodCapEvaluator.EvaluateAsync(group.Key, clientName, startDate, analyseToken, cancellationToken));
            allEntries.AddRange(await restDayRotationEvaluator.EvaluateAsync(group.Key, clientName, endDate, analyseToken, cancellationToken));
            allEntries.AddRange(await counterRuleEvaluator.EvaluateAsync(group.Key, clientName, endDate, analyseToken, cancellationToken));
            allEntries.AddRange(await restrictedTimeWindowEvaluator.EvaluateRangeAsync(group.Key, clientName, startDate, endDate, analyseToken, cancellationToken));

            // Reconcile over [startDate - boundary, endDate] so cross-boundary gaps and fulfilment are
            // caught even when nobody re-edits the trigger date; then surface the open obligations.
            await compensatoryRestReconciler.ReconcileAsync(group.Key, startDate.AddDays(-ScenarioConstants.BoundaryDays), endDate, analyseToken, cancellationToken);
            allEntries.AddRange(await compensatoryRestEvaluator.EvaluateAsync(group.Key, clientName, endDate, analyseToken, cancellationToken));

            if (travelLookupBudget <= 0)
            {
                clientsWithoutTravelCheck++;
            }
            else
            {
                try
                {
                    travelLookupBudget -= await AddTravelTimeEntriesAsync(allEntries, timeline, clientName, shiftAddressLookup, travelTimeService, travelApiKeyConfigured, cancellationToken, travelLookupBudget);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Travel time check failed for Client {ClientId} in range {Start} - {End}", group.Key, startDate, endDate);
                }
            }

            allCollisions.AddRange(BuildLegacyCollisionList(timeline, clientNameLookup));
        }

        if (clientsWithoutTravelCheck > 0)
        {
            _logger.LogWarning(
                "Travel time budget of {Budget} lookups exhausted for range {Start} - {End}: {ClientCount} client(s) were not checked for travel time",
                MaxTravelTimeLookupsPerRangeCheck, startDate, endDate, clientsWithoutTravelCheck);
        }

        await AddQualificationEntriesAsync(allEntries, works, clientNameLookup, eligibilityMatrixBuilder, cancellationToken);

        _logger.LogDebug("[COLLISION-TRACE] RangeCheck results: {CollisionCount} collisions, {ValidationCount} validations, {ClientCount} clients checked, {WorkCount} works, {BreakCount} breaks",
            allCollisions.Count, allEntries.Count, groupedByClient.Count(), works.Count, breaks.Count);

        var collisionNotification = new CollisionListNotificationDto
        {
            Collisions = allCollisions,
            IsFullRefresh = true,
            AnalyseToken = analyseToken
        };
        await notificationService.NotifyCollisionsDetected(collisionNotification);
        _logger.LogDebug("[COLLISION-TRACE] NotifyCollisionsDetected SENT ({Count} collisions)", allCollisions.Count);

        await escalationService.EscalateBlockedViolationsAsync(allEntries);

        var validationNotification = new ScheduleValidationListNotificationDto
        {
            Entries = allEntries,
            IsFullRefresh = true,
            AnalyseToken = analyseToken
        };
        await notificationService.NotifyScheduleValidationsDetected(validationNotification);
        _logger.LogDebug("[COLLISION-TRACE] NotifyScheduleValidationsDetected SENT ({Count} entries)", allEntries.Count);
    }

    /// <summary>
    /// Notification-only qualification check for the live editing path: for every placed work, report
    /// any required-qualification gap of the assigned agent as a schedule-validation entry. A completely
    /// missing mandatory qualification is an Error; too-low level, expired or optional gaps are Warnings.
    /// Never blocks the save — it mirrors how collisions/overtime are surfaced post-commit.
    /// </summary>
    private static async Task AddQualificationEntriesAsync(
        List<ScheduleValidationNotificationDto> entries,
        IReadOnlyList<Work> works,
        IReadOnlyDictionary<Guid, string> clientNameLookup,
        IEligibilityMatrixBuilder eligibilityMatrixBuilder,
        CancellationToken cancellationToken)
    {
        var relevant = works.Where(w => w.ShiftId != Guid.Empty).ToList();
        if (relevant.Count == 0)
        {
            return;
        }

        var agentIds = relevant.Select(w => w.ClientId).Distinct().ToList();
        var slots = relevant
            .Select(w => new EligibilitySlot(w.ShiftId, w.CurrentDate))
            .Distinct()
            .ToList();

        var matrix = await eligibilityMatrixBuilder.BuildAsync(agentIds, slots, ct: cancellationToken);
        if (matrix.Gaps.Count == 0)
        {
            return;
        }

        foreach (var work in relevant)
        {
            var key = (work.ClientId.ToString(), work.ShiftId, work.CurrentDate);
            if (!matrix.Gaps.TryGetValue(key, out var gaps))
            {
                continue;
            }

            clientNameLookup.TryGetValue(work.ClientId, out var clientName);
            foreach (var gap in gaps)
            {
                entries.Add(new ScheduleValidationNotificationDto
                {
                    Type = gap.Severity == QualificationGapSeverity.Error
                        ? ScheduleValidationType.Error
                        : ScheduleValidationType.Warning,
                    ClientId = work.ClientId,
                    ClientName = clientName ?? string.Empty,
                    Date = work.CurrentDate,
                    Comment = QualificationValidationKeys.ForReason(gap.Reason),
                    CommentParams = new Dictionary<string, string>
                    {
                        ["qualificationId"] = gap.QualificationId.ToString(),
                        ["minLevel"] = gap.RequiredMinLevel.ToString(),
                    },
                });
            }
        }
    }

    /// <summary>
    /// Runs the checks that need more than the edited day. The surrounding single check only loads that
    /// one day, which is too narrow for a weekly cap, a weekly rest-day quota or a run of consecutive
    /// days, so this loads the full ISO week separately.
    /// All three report inside the edited day's ISO week, which is the window the client retracts when
    /// that week is re-evaluated: the two weekly checks anchor on the week's Monday exactly like the
    /// range check, the consecutive-day check anchors on the day its run starts. GetConsecutiveWorkDays
    /// counts forward, so a run reaching into the week from before Monday is under-counted here — never
    /// over-counted — and the range check, which sees the whole loaded period, reports it in full.
    /// </summary>
    /// <param name="date">The edited day; its ISO week defines the load window</param>
    /// <param name="policy">Supplies MaxWeeklyHours, MinRestDays and MaxConsecutiveDays for the client</param>
    private static async Task AddWeekAwareEntriesAsync(
        DataBaseContext dbContext,
        ITimelineCalculationService timelineCalculationService,
        List<ScheduleValidationNotificationDto> entries,
        Guid clientId,
        string clientName,
        DateOnly date,
        Guid? analyseToken,
        SchedulingPolicy policy,
        CancellationToken cancellationToken)
    {
        var (weekStart, weekEnd) = ScheduleValidationBuilder.IsoWeekOf(date);

        var ownWorks = await dbContext.Work
            .AsNoTracking()
            .Where(w => w.ClientId == clientId && w.CurrentDate >= weekStart && w.CurrentDate <= weekEnd &&
                        !w.IsDeleted && w.ParentWorkId == null && w.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var worksWithReplacementForClient = await dbContext.Work
            .AsNoTracking()
            .Where(w => w.CurrentDate >= weekStart && w.CurrentDate <= weekEnd && !w.IsDeleted &&
                        w.ParentWorkId == null && w.AnalyseToken == analyseToken &&
                        dbContext.WorkChange.Any(wc =>
                            wc.WorkId == w.Id && !wc.IsDeleted && wc.ReplaceClientId == clientId))
            .ToListAsync(cancellationToken);

        var allWorks = ownWorks
            .Union(worksWithReplacementForClient, new WorkIdComparer())
            .ToList();

        var workIds = allWorks.Select(w => w.Id).ToList();
        var workChanges = workIds.Count > 0
            ? await dbContext.WorkChange
                .AsNoTracking()
                .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
                .ToListAsync(cancellationToken)
            : [];

        // Breaks must be loaded over the same window: a break day counts as a rest day, so a narrower
        // break load would make AddMinRestDays under-count rest days and report false positives.
        var breaks = await dbContext.Break
            .AsNoTracking()
            .Where(b => b.ClientId == clientId && b.CurrentDate >= weekStart && b.CurrentDate <= weekEnd &&
                        !b.IsDeleted && b.ParentWorkId == null && b.AnalyseToken == analyseToken)
            .ToListAsync(cancellationToken);

        var weekBlocks = timelineCalculationService
            .CalculateScheduleBlocks(allWorks, workChanges, breaks)
            .Where(b => b.ClientId == clientId);

        var weekTimeline = new ClientTimeline(clientId);
        weekTimeline.AddBlocks(weekBlocks);
        weekTimeline.SortBlocks();

        ScheduleValidationBuilder.AddWeeklyOvertime(entries, weekTimeline, clientName, weekStart, weekEnd, policy);
        ScheduleValidationBuilder.AddMinRestDays(entries, weekTimeline, clientName, weekStart, weekEnd, policy);
        ScheduleValidationBuilder.AddConsecutiveDays(entries, weekTimeline, clientName, weekStart, weekEnd, policy);
    }

    /// <summary>
    /// Flags consecutive shifts of one client whose gap is shorter than the travel time between their
    /// locations. Only pairs starting on the same day are compared: an overnight transition is already
    /// covered by the rest-period check, and pairing across days would invent gaps that are not one shift
    /// following another.
    /// </summary>
    /// <param name="lookupBudget">Maximum routing lookups this call may spend</param>
    /// <returns>Number of routing lookups actually spent</returns>
    private static async Task<int> AddTravelTimeEntriesAsync(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        Dictionary<Guid, Address?> shiftAddressLookup,
        ITravelTimeCalculationService travelTimeService,
        bool apiKeyConfigured,
        CancellationToken cancellationToken,
        int lookupBudget = int.MaxValue)
    {
        var workBlocks = timeline.Blocks
            .Where(b => b.BlockType == ScheduleBlockType.Work && b.ShiftId.HasValue)
            .OrderBy(b => b.Start)
            .ToList();

        if (workBlocks.Count < 2) return 0;

        var noApiKeyInfoAdded = false;
        var lookupsSpent = 0;

        for (var i = 0; i < workBlocks.Count - 1; i++)
        {
            var current = workBlocks[i];
            var next = workBlocks[i + 1];

            if (current.OwnerDate != next.OwnerDate) continue;

            shiftAddressLookup.TryGetValue(current.ShiftId!.Value, out var fromAddress);
            shiftAddressLookup.TryGetValue(next.ShiftId!.Value, out var toAddress);

            if (fromAddress == null || toAddress == null) continue;
            if (IsSameAddress(fromAddress, toAddress)) continue;

            if (!apiKeyConfigured)
            {
                if (!noApiKeyInfoAdded)
                {
                    entries.Add(new ScheduleValidationNotificationDto
                    {
                        Type = ScheduleValidationType.Info,
                        ClientId = timeline.ClientId,
                        ClientName = clientName,
                        Date = current.OwnerDate,
                        Comment = "schedule.error-list.travel-time-no-api-key"
                    });
                    noApiKeyInfoAdded = true;
                }
                continue;
            }

            if (lookupsSpent >= lookupBudget) break;

            var gap = current.GapTo(next);
            var travelTime = await travelTimeService.CalculateTravelTimeAsync(fromAddress, toAddress, cancellationToken);
            lookupsSpent++;

            if (!travelTime.HasValue) continue;

            if (gap < travelTime.Value)
            {
                entries.Add(new ScheduleValidationNotificationDto
                {
                    Type = ScheduleValidationType.Error,
                    ClientId = timeline.ClientId,
                    ClientName = clientName,
                    Date = current.OwnerDate,
                    Comment = "schedule.error-list.travel-time-error",
                    CommentParams = new Dictionary<string, string>
                    {
                        ["gapMinutes"] = $"{gap.TotalMinutes:F0}",
                        ["travelMinutes"] = $"{travelTime.Value.TotalMinutes:F0}"
                    }
                });
            }
            else if (gap < TimeSpan.FromTicks((long)(travelTime.Value.Ticks * TravelTimeWarningFactor)))
            {
                entries.Add(new ScheduleValidationNotificationDto
                {
                    Type = ScheduleValidationType.Warning,
                    ClientId = timeline.ClientId,
                    ClientName = clientName,
                    Date = current.OwnerDate,
                    Comment = "schedule.error-list.travel-time-warning",
                    CommentParams = new Dictionary<string, string>
                    {
                        ["gapMinutes"] = $"{gap.TotalMinutes:F0}",
                        ["travelMinutes"] = $"{travelTime.Value.TotalMinutes:F0}"
                    }
                });
            }
        }

        return lookupsSpent;
    }

    private static bool IsSameAddress(Address a, Address b)
    {
        if (a.Id == b.Id) return true;
        return string.Equals(a.Street, b.Street, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Zip, b.Zip, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.City, b.City, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads the shift locations for a range check. The single check gets them through its work query's
    /// includes, but a range check spans every client of the period — including the shift/client/address
    /// graph there would weigh down the main query, so the addresses are fetched once for the distinct
    /// shifts instead.
    /// </summary>
    private static async Task<Dictionary<Guid, Address?>> LoadShiftAddressLookupAsync(
        DataBaseContext dbContext,
        List<Work> works,
        CancellationToken cancellationToken)
    {
        var shiftIds = works
            .Select(w => w.ShiftId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (shiftIds.Count == 0) return [];

        var shifts = await dbContext.Shift
            .AsNoTracking()
            .Include(s => s.Client).ThenInclude(c => c!.Addresses)
            .Where(s => shiftIds.Contains(s.Id) && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        var lookup = new Dictionary<Guid, Address?>();
        foreach (var shift in shifts)
        {
            lookup[shift.Id] = shift.Client?.Addresses
                .OrderByDescending(a => a.ValidFrom)
                .FirstOrDefault();
        }
        return lookup;
    }

    private static Dictionary<Guid, Address?> BuildShiftAddressLookup(List<Work> works)
    {
        var lookup = new Dictionary<Guid, Address?>();
        foreach (var work in works)
        {
            if (lookup.ContainsKey(work.ShiftId)) continue;
            var address = work.Shift?.Client?.Addresses
                .OrderByDescending(a => a.ValidFrom)
                .FirstOrDefault();
            lookup[work.ShiftId] = address;
        }
        return lookup;
    }

    private static CollisionListNotificationDto BuildLegacyCollisionNotification(
        ClientTimeline timeline,
        Dictionary<Guid, string> clientNameLookup,
        bool isFullRefresh,
        Guid? checkedClientId,
        DateOnly? checkedDate,
        Guid? analyseToken)
    {
        return new CollisionListNotificationDto
        {
            Collisions = BuildLegacyCollisionList(timeline, clientNameLookup),
            IsFullRefresh = isFullRefresh,
            CheckedClientId = checkedClientId,
            CheckedDate = checkedDate,
            AnalyseToken = analyseToken
        };
    }

    private static List<CollisionNotificationDto> BuildLegacyCollisionList(
        ClientTimeline timeline,
        Dictionary<Guid, string> clientNameLookup)
    {
        var pairs = timeline.GetCollisions();
        if (pairs.Count == 0) return [];

        clientNameLookup.TryGetValue(timeline.ClientId, out var clientName);
        clientName ??= string.Empty;

        return pairs.Select(p => new CollisionNotificationDto
        {
            WorkId1 = p.A.SourceId,
            WorkId2 = p.B.SourceId,
            ClientId = timeline.ClientId,
            ClientName = clientName,
            Date = p.A.OwnerDate,
            TimeRange1 = $"{p.A.Start:HH:mm} - {p.A.End:HH:mm}",
            TimeRange2 = $"{p.B.Start:HH:mm} - {p.B.End:HH:mm}",
            BlockType1 = p.A.BlockType.ToString(),
            BlockType2 = p.B.BlockType.ToString()
        }).ToList();
    }

    private static Dictionary<Guid, string> BuildClientNameLookup(List<Work> works, List<WorkChange> workChanges)
    {
        var lookup = new Dictionary<Guid, string>();
        foreach (var work in works)
        {
            if (work.Client != null && !lookup.ContainsKey(work.ClientId))
            {
                lookup[work.ClientId] = $"{work.Client.Name} {work.Client.FirstName}".Trim();
            }
        }
        foreach (var wc in workChanges)
        {
            if (wc.ReplaceClient != null && wc.ReplaceClientId.HasValue && !lookup.ContainsKey(wc.ReplaceClientId.Value))
            {
                lookup[wc.ReplaceClientId.Value] = $"{wc.ReplaceClient.Name} {wc.ReplaceClient.FirstName}".Trim();
            }
        }
        return lookup;
    }
}

file class WorkIdComparer : IEqualityComparer<Work>
{
    public bool Equals(Work? x, Work? y) => x?.Id == y?.Id;
    public int GetHashCode(Work obj) => obj.Id.GetHashCode();
}
