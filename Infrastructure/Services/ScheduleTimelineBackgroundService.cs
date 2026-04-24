// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background worker for schedule validation: collisions, rest periods, working hours, travel times.
/// Receives check requests via Channel and sends results via SignalR.
/// </summary>
using System.Net.Sockets;
using System.Threading.Channels;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Infrastructure.Services;

public class ScheduleTimelineBackgroundService : BackgroundService, IScheduleTimelineService
{
    private static readonly TimeSpan MinRestDuration = TimeSpan.FromHours(11);
    private static readonly TimeSpan MaxDailyWorkDuration = TimeSpan.FromHours(10);
    private const int MaxConsecutiveWorkDays = 6;
    private const double TravelTimeWarningFactor = 1.5;

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

        _channel = Channel.CreateUnbounded<TimelineCheckRequest>(new UnboundedChannelOptions
        {
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

                if (job.IsRangeCheck)
                {
                    _logger.LogDebug("[COLLISION-TRACE] START RangeCheck {Start} - {End} token={Token}",
                        job.StartDate, job.EndDate, job.AnalyseToken?.ToString() ?? "null");
                    await ProcessRangeCheckAsync(dbContext, notificationService, timelineCalculationService, job.StartDate, job.EndDate, job.AnalyseToken, stoppingToken);
                    _logger.LogDebug("[COLLISION-TRACE] DONE RangeCheck {Start} - {End} token={Token}",
                        job.StartDate, job.EndDate, job.AnalyseToken?.ToString() ?? "null");
                }
                else
                {
                    _logger.LogDebug("[COLLISION-TRACE] START SingleCheck Client={ClientId} Date={Date} token={Token}",
                        job.ClientId, job.Date, job.AnalyseToken?.ToString() ?? "null");
                    await ProcessSingleCheckAsync(dbContext, notificationService, timelineCalculationService, travelTimeService, job.ClientId, job.Date, job.AnalyseToken, stoppingToken);
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

        AddRestViolationEntries(entries, timeline, clientName);
        AddOvertimeEntries(entries, timeline, clientName, date, date);
        AddConsecutiveDayEntries(entries, timeline, clientName, date, date);

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

        var groupedByClient = scheduleBlocks.GroupBy(b => b.ClientId);
        var allEntries = new List<ScheduleValidationNotificationDto>();
        var allCollisions = new List<CollisionNotificationDto>();

        foreach (var group in groupedByClient)
        {
            var timeline = new ClientTimeline(group.Key);
            timeline.AddBlocks(group);
            timeline.SortBlocks();
            _timelineStore.SetTimeline(group.Key, timeline);

            clientNameLookup.TryGetValue(group.Key, out var clientName);
            clientName ??= string.Empty;

            AddRestViolationEntries(allEntries, timeline, clientName);
            AddOvertimeEntries(allEntries, timeline, clientName, startDate, endDate);
            AddConsecutiveDayEntries(allEntries, timeline, clientName, startDate, endDate);

            allCollisions.AddRange(BuildLegacyCollisionList(timeline, clientNameLookup));
        }

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

        var validationNotification = new ScheduleValidationListNotificationDto
        {
            Entries = allEntries,
            IsFullRefresh = true,
            AnalyseToken = analyseToken
        };
        await notificationService.NotifyScheduleValidationsDetected(validationNotification);
        _logger.LogDebug("[COLLISION-TRACE] NotifyScheduleValidationsDetected SENT ({Count} entries)", allEntries.Count);
    }

    private static void AddRestViolationEntries(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName)
    {
        foreach (var violation in timeline.GetRestViolations(MinRestDuration))
        {
            entries.Add(new ScheduleValidationNotificationDto
            {
                Type = ScheduleValidationType.Warning,
                ClientId = timeline.ClientId,
                ClientName = clientName,
                Date = violation.PreviousBlock.OwnerDate,
                Comment = "schedule.error-list.rest-violation",
                CommentParams = new Dictionary<string, string>
                {
                    ["actualHours"] = $"{violation.ActualRest.TotalHours:F1}",
                    ["requiredHours"] = $"{violation.RequiredRest.TotalHours:F0}",
                    ["endTime"] = $"{violation.PreviousBlock.End:HH:mm}",
                    ["startTime"] = $"{violation.NextBlock.Start:HH:mm}"
                }
            });
        }
    }

    private static void AddOvertimeEntries(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        DateOnly startDate,
        DateOnly endDate)
    {
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var duration = timeline.GetWorkDuration(date);
            if (duration > MaxDailyWorkDuration)
            {
                entries.Add(new ScheduleValidationNotificationDto
                {
                    Type = ScheduleValidationType.Warning,
                    ClientId = timeline.ClientId,
                    ClientName = clientName,
                    Date = date,
                    Comment = "schedule.error-list.overtime",
                    CommentParams = new Dictionary<string, string>
                    {
                        ["actualHours"] = $"{duration.TotalHours:F1}",
                        ["maxHours"] = $"{MaxDailyWorkDuration.TotalHours:F0}"
                    }
                });
            }
        }
    }

    private static void AddConsecutiveDayEntries(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        DateOnly startDate,
        DateOnly endDate)
    {
        var date = startDate;
        while (date <= endDate)
        {
            var consecutive = timeline.GetConsecutiveWorkDays(date);
            if (consecutive > MaxConsecutiveWorkDays)
            {
                entries.Add(new ScheduleValidationNotificationDto
                {
                    Type = ScheduleValidationType.Warning,
                    ClientId = timeline.ClientId,
                    ClientName = clientName,
                    Date = date,
                    Comment = "schedule.error-list.consecutive-days",
                    CommentParams = new Dictionary<string, string>
                    {
                        ["actualDays"] = consecutive.ToString(),
                        ["maxDays"] = MaxConsecutiveWorkDays.ToString()
                    }
                });
                date = date.AddDays(consecutive);
            }
            else
            {
                date = date.AddDays(1);
            }
        }
    }

    private static async Task AddTravelTimeEntriesAsync(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        Dictionary<Guid, Address?> shiftAddressLookup,
        ITravelTimeCalculationService travelTimeService,
        bool apiKeyConfigured,
        CancellationToken cancellationToken)
    {
        var workBlocks = timeline.Blocks
            .Where(b => b.BlockType == ScheduleBlockType.Work && b.ShiftId.HasValue)
            .OrderBy(b => b.Start)
            .ToList();

        if (workBlocks.Count < 2) return;

        var noApiKeyInfoAdded = false;

        for (var i = 0; i < workBlocks.Count - 1; i++)
        {
            var current = workBlocks[i];
            var next = workBlocks[i + 1];

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

            var gap = current.GapTo(next);
            var travelTime = await travelTimeService.CalculateTravelTimeAsync(fromAddress, toAddress, cancellationToken);

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
    }

    private static bool IsSameAddress(Address a, Address b)
    {
        if (a.Id == b.Id) return true;
        return string.Equals(a.Street, b.Street, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Zip, b.Zip, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.City, b.City, StringComparison.OrdinalIgnoreCase);
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
