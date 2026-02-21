using System.Threading.Channels;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
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

                    if (request.IsRangeCheck)
                    {
                        await ProcessRangeCheckAsync(dbContext, notificationService, request.StartDate, request.EndDate, stoppingToken);
                    }
                    else
                    {
                        await ProcessSingleCheckAsync(dbContext, notificationService, request.ClientId, request.Date, stoppingToken);
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
        var timeRects = CalculateTimeRects(allWorks, workChanges, breaks);
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
        var timeRects = CalculateTimeRects(works, workChanges, breaks);
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

    internal static List<TimeRect> CalculateTimeRects(List<Work> works, List<WorkChange> workChanges, List<Break> breaks)
    {
        var result = new List<TimeRect>();
        var changesByWorkId = workChanges.GroupBy(wc => wc.WorkId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var work in works)
        {
            var effectiveStart = work.StartTime;
            var effectiveEnd = work.EndTime;

            if (changesByWorkId.TryGetValue(work.Id, out var changes))
            {
                foreach (var change in changes)
                {
                    switch (change.Type)
                    {
                        case WorkChangeType.CorrectionStart:
                            effectiveStart = change.StartTime;
                            break;
                        case WorkChangeType.CorrectionEnd:
                            effectiveEnd = change.EndTime;
                            break;
                        case WorkChangeType.ReplacementStart:
                            effectiveStart = change.EndTime;
                            break;
                        case WorkChangeType.ReplacementEnd:
                            effectiveEnd = change.StartTime;
                            break;
                    }
                }

                foreach (var change in changes.Where(c =>
                    c.Type is WorkChangeType.CorrectionStart or WorkChangeType.CorrectionEnd))
                {
                    AddRectsWithMidnightSplit(result, work.Id, TimeRectSourceType.Correction,
                        work.ClientId, work.CurrentDate, change.StartTime, change.EndTime);
                }

                foreach (var change in changes.Where(c =>
                    c.Type is WorkChangeType.ReplacementStart or WorkChangeType.ReplacementEnd &&
                    c.ReplaceClientId.HasValue))
                {
                    AddRectsWithMidnightSplit(result, work.Id, TimeRectSourceType.Replacement,
                        change.ReplaceClientId!.Value, work.CurrentDate, change.StartTime, change.EndTime);
                }
            }

            AddRectsWithMidnightSplit(result, work.Id, TimeRectSourceType.Work,
                work.ClientId, work.CurrentDate, effectiveStart, effectiveEnd);
        }

        foreach (var b in breaks)
        {
            AddRectsWithMidnightSplit(result, b.Id, TimeRectSourceType.Break,
                b.ClientId, b.CurrentDate, b.StartTime, b.EndTime);
        }

        return result;
    }

    private static void AddRectsWithMidnightSplit(
        List<TimeRect> result,
        Guid sourceId,
        TimeRectSourceType sourceType,
        Guid clientId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end)
    {
        if (end < start)
        {
            result.Add(new TimeRect(sourceId, sourceType, clientId, date, start, TimeOnly.MaxValue));
            result.Add(new TimeRect(sourceId, sourceType, clientId, date.AddDays(1), TimeOnly.MinValue, end));
        }
        else
        {
            result.Add(new TimeRect(sourceId, sourceType, clientId, date, start, end));
        }
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
