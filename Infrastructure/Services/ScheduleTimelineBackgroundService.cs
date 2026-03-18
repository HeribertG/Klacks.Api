// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background-Worker für Schedule-Validierung: Kollisionen, Ruhezeiten, Arbeitszeiten, Reisezeiten.
/// Empfängt Check-Requests via Channel und sendet Ergebnisse via SignalR.
/// </summary>
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

namespace Klacks.Api.Infrastructure.Services;

public class ScheduleTimelineBackgroundService : BackgroundService, IScheduleTimelineService
{
    private static readonly TimeSpan MinRestDuration = TimeSpan.FromHours(11);
    private static readonly TimeSpan MaxDailyWorkDuration = TimeSpan.FromHours(10);
    private const int MaxConsecutiveWorkDays = 6;
    private const double TravelTimeWarningFactor = 1.5;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduleTimelineBackgroundService> _logger;
    private readonly IScheduleTimelineStore _timelineStore;
    private readonly Channel<TimelineCheckRequest> _channel;
    private volatile CancellationTokenSource _processingCts = new();

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

    public void QueueCheck(Guid clientId, DateOnly date)
    {
        var request = new TimelineCheckRequest
        {
            ClientId = clientId,
            Date = date
        };

        if (!_channel.Writer.TryWrite(request))
        {
            _logger.LogWarning(
                "Failed to queue timeline check for Client {ClientId} on {Date}",
                clientId, date);
        }
    }

    public void QueueRangeCheck(DateOnly startDate, DateOnly endDate)
    {
        _logger.LogDebug("[COLLISION-TRACE] QueueRangeCheck called {Start} - {End}, cancelling current", startDate, endDate);
        CancelCurrentProcessing();

        var request = new TimelineCheckRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            IsRangeCheck = true
        };

        if (!_channel.Writer.TryWrite(request))
        {
            _logger.LogWarning(
                "Failed to queue timeline range check for {StartDate} to {EndDate}",
                startDate, endDate);
        }
    }

    private void CancelCurrentProcessing()
    {
        try { _processingCts.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduleTimelineBackgroundService started");

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                var latestRequest = request;
                var skipped = 0;
                while (_channel.Reader.TryRead(out var newer))
                {
                    latestRequest = newer;
                    skipped++;
                }

                if (skipped > 0)
                {
                    _logger.LogDebug("[COLLISION-TRACE] Skipped {Count} stale timeline check requests", skipped);
                }

                _processingCts = new CancellationTokenSource();
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _processingCts.Token);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<IWorkNotificationService>();
                    var timelineCalculationService = scope.ServiceProvider.GetRequiredService<ITimelineCalculationService>();
                    var travelTimeService = scope.ServiceProvider.GetRequiredService<ITravelTimeCalculationService>();

                    if (latestRequest.IsRangeCheck)
                    {
                        _logger.LogDebug("[COLLISION-TRACE] START RangeCheck {Start} - {End}", latestRequest.StartDate, latestRequest.EndDate);
                        await ProcessRangeCheckAsync(dbContext, notificationService, timelineCalculationService, latestRequest.StartDate, latestRequest.EndDate, linkedCts.Token);
                        _logger.LogDebug("[COLLISION-TRACE] DONE RangeCheck {Start} - {End}", latestRequest.StartDate, latestRequest.EndDate);
                    }
                    else
                    {
                        _logger.LogDebug("[COLLISION-TRACE] START SingleCheck Client={ClientId} Date={Date}", latestRequest.ClientId, latestRequest.Date);
                        await ProcessSingleCheckAsync(dbContext, notificationService, timelineCalculationService, travelTimeService, latestRequest.ClientId, latestRequest.Date, linkedCts.Token);
                        _logger.LogDebug("[COLLISION-TRACE] DONE SingleCheck Client={ClientId}", latestRequest.ClientId);
                    }
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("[COLLISION-TRACE] CANCELLED - newer request arrived");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing timeline check request");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("ScheduleTimelineBackgroundService stopped");
    }

    private async Task ProcessSingleCheckAsync(
        DataBaseContext dbContext,
        IWorkNotificationService notificationService,
        ITimelineCalculationService timelineCalculationService,
        ITravelTimeCalculationService travelTimeService,
        Guid clientId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var ownWorks = await dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Include(w => w.Shift).ThenInclude(s => s!.Client).ThenInclude(c => c!.Addresses)
            .Where(w => w.ClientId == clientId && w.CurrentDate == date && !w.IsDeleted)
            .ToListAsync(cancellationToken);

        var worksWithReplacementForClient = await dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Include(w => w.Shift).ThenInclude(s => s!.Client).ThenInclude(c => c!.Addresses)
            .Where(w => w.CurrentDate == date && !w.IsDeleted &&
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
            .Where(b => b.ClientId == clientId && b.CurrentDate == date && !b.IsDeleted)
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

        var collisionNotification = BuildLegacyCollisionNotification(timeline, clientNameLookup, false, clientId, date);
        await notificationService.NotifyCollisionsDetected(collisionNotification);

        var validationNotification = new ScheduleValidationListNotificationDto
        {
            Entries = entries,
            IsFullRefresh = false,
            CheckedClientId = clientId,
            CheckedDate = date
        };
        await notificationService.NotifyScheduleValidationsDetected(validationNotification);
    }

    private async Task ProcessRangeCheckAsync(
        DataBaseContext dbContext,
        IWorkNotificationService notificationService,
        ITimelineCalculationService timelineCalculationService,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var works = await dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Where(w => w.CurrentDate >= startDate && w.CurrentDate <= endDate && !w.IsDeleted)
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
            .Where(b => b.CurrentDate >= startDate && b.CurrentDate <= endDate && !b.IsDeleted)
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

        _logger.LogDebug("[COLLISION-TRACE] RangeCheck results: {CollisionCount} collisions, {ValidationCount} validations, {ClientCount} clients checked",
            allCollisions.Count, allEntries.Count, groupedByClient.Count());

        var collisionNotification = new CollisionListNotificationDto
        {
            Collisions = allCollisions,
            IsFullRefresh = true
        };
        await notificationService.NotifyCollisionsDetected(collisionNotification);
        _logger.LogDebug("[COLLISION-TRACE] NotifyCollisionsDetected SENT ({Count} collisions)", allCollisions.Count);

        var validationNotification = new ScheduleValidationListNotificationDto
        {
            Entries = allEntries,
            IsFullRefresh = true
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
        DateOnly? checkedDate)
    {
        return new CollisionListNotificationDto
        {
            Collisions = BuildLegacyCollisionList(timeline, clientNameLookup),
            IsFullRefresh = isFullRefresh,
            CheckedClientId = checkedClientId,
            CheckedDate = checkedDate
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
            TimeRange2 = $"{p.B.Start:HH:mm} - {p.B.End:HH:mm}"
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
