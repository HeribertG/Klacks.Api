// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background-Worker für Schedule-Validierung: Kollisionen, Ruhezeiten, Arbeitszeiten.
/// Empfängt Check-Requests via Channel und sendet Ergebnisse via SignalR.
/// </summary>
using System.Threading.Channels;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services;

public class ScheduleTimelineBackgroundService : BackgroundService, IScheduleTimelineService
{
    private static readonly TimeSpan MinRestDuration = TimeSpan.FromHours(11);
    private static readonly TimeSpan MaxDailyWorkDuration = TimeSpan.FromHours(10);
    private const int MaxConsecutiveWorkDays = 6;

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduleTimelineBackgroundService started");

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<IWorkNotificationService>();
                    var timelineCalculationService = scope.ServiceProvider.GetRequiredService<ITimelineCalculationService>();

                    if (request.IsRangeCheck)
                    {
                        await ProcessRangeCheckAsync(dbContext, notificationService, timelineCalculationService, request.StartDate, request.EndDate, stoppingToken);
                    }
                    else
                    {
                        await ProcessSingleCheckAsync(dbContext, notificationService, timelineCalculationService, request.ClientId, request.Date, stoppingToken);
                    }
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
        Guid clientId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var ownWorks = await dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Where(w => w.ClientId == clientId && w.CurrentDate == date && !w.IsDeleted)
            .ToListAsync(cancellationToken);

        var worksWithReplacementForClient = await dbContext.Work
            .AsNoTracking()
            .Include(w => w.Client)
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

        AddCollisionEntries(entries, timeline, clientName);
        AddRestViolationEntries(entries, timeline, clientName);
        AddOvertimeEntries(entries, timeline, clientName, date, date);
        AddConsecutiveDayEntries(entries, timeline, clientName, date, date);

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

            AddCollisionEntries(allEntries, timeline, clientName);
            AddRestViolationEntries(allEntries, timeline, clientName);
            AddOvertimeEntries(allEntries, timeline, clientName, startDate, endDate);
            AddConsecutiveDayEntries(allEntries, timeline, clientName, startDate, endDate);

            allCollisions.AddRange(BuildLegacyCollisionList(timeline, clientNameLookup));
        }

        var collisionNotification = new CollisionListNotificationDto
        {
            Collisions = allCollisions,
            IsFullRefresh = true
        };
        await notificationService.NotifyCollisionsDetected(collisionNotification);

        var validationNotification = new ScheduleValidationListNotificationDto
        {
            Entries = allEntries,
            IsFullRefresh = true
        };
        await notificationService.NotifyScheduleValidationsDetected(validationNotification);
    }

    private static void AddCollisionEntries(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName)
    {
        foreach (var (a, b) in timeline.GetCollisions())
        {
            entries.Add(new ScheduleValidationNotificationDto
            {
                Type = "error",
                ClientId = timeline.ClientId,
                ClientName = clientName,
                Date = a.OwnerDate,
                Comment = "schedule.error-list.collision",
                CommentParams = new Dictionary<string, string>
                {
                    ["timeRange1"] = $"{a.Start:HH:mm} - {a.End:HH:mm}",
                    ["timeRange2"] = $"{b.Start:HH:mm} - {b.End:HH:mm}"
                }
            });
        }
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
                Type = "warning",
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
                    Type = "warning",
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
                    Type = "warning",
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
