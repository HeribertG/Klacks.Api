// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
        var timeRects = timelineCalculationService.CalculateTimeRects(allWorks, workChanges, breaks);
        var clientRects = timeRects.Where(r => r.ClientId == clientId).ToList();

        var timeline = new ClientDayTimeline(clientId, date);
        timeline.Rects.AddRange(clientRects);
        _timelineStore.SetTimeline(clientId, date, timeline);

        var collisions = BuildCollisionNotifications(timeline, clientNameLookup);

        var notification = new CollisionListNotificationDto
        {
            Collisions = collisions,
            IsFullRefresh = false,
            CheckedClientId = clientId,
            CheckedDate = date
        };
        await notificationService.NotifyCollisionsDetected(notification);
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
        var timeRects = timelineCalculationService.CalculateTimeRects(works, workChanges, breaks);
        var allCollisions = new List<CollisionNotificationDto>();

        var groupedByClientDate = timeRects.GroupBy(r => new { r.ClientId, r.Date });

        foreach (var group in groupedByClientDate)
        {
            var timeline = new ClientDayTimeline(group.Key.ClientId, group.Key.Date);
            timeline.Rects.AddRange(group);
            _timelineStore.SetTimeline(group.Key.ClientId, group.Key.Date, timeline);

            var collisions = BuildCollisionNotifications(timeline, clientNameLookup);
            allCollisions.AddRange(collisions);
        }

        var notification = new CollisionListNotificationDto
        {
            Collisions = allCollisions,
            IsFullRefresh = true
        };
        await notificationService.NotifyCollisionsDetected(notification);
    }

    private static List<CollisionNotificationDto> BuildCollisionNotifications(
        ClientDayTimeline timeline,
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
            Date = timeline.Date,
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
