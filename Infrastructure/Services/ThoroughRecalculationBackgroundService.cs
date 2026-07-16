// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background service that performs a thorough recalculation of schedule entries in a date range.
/// Reprocesses Work, WorkChange and Break macros, then recalculates all client period hours.
/// Sealed entries (LockLevel != None) are never overwritten and are reported via warning log.
/// A request may carry an optional client scope (null/empty = all clients); scoped requests
/// reprocess only the works, work changes and breaks of those clients and pull the period hours
/// of exactly those clients afterwards. The scoped period-hours path calls
/// RecalculatePeriodHoursAsync per client and thereby intentionally bypasses the
/// membership-overlap filter that RecalculateAllClientsAsync applies: when works of a former
/// member inside the window were just reprocessed, the period-hours pull must follow them,
/// otherwise that client's ClientPeriodHours would stay inconsistent with the freshly updated
/// work surcharges. Identical pending requests (same window, group, token and
/// client set regardless of the order the ids were passed in) are coalesced; when the queue is
/// full a new request is rejected visibly (return value false plus warning log) instead of
/// silently dropping an older one. Notifies clients via SignalR when finished.
/// </summary>
/// <param name="serviceProvider">Root service provider for scoped resolution per queued recalculation request</param>
/// <param name="logger">Logger for diagnostics</param>

using System.Threading.Channels;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services;

public class ThoroughRecalculationBackgroundService : BackgroundService, IThoroughRecalculationQueue
{
    internal const int ChannelCapacity = 20;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ThoroughRecalculationBackgroundService> _logger;
    private readonly Channel<ThoroughRecalculationRequest> _channel;
    private readonly HashSet<ThoroughRecalculationRequest> _pendingRequests = [];
    private readonly object _pendingLock = new();

    public ThoroughRecalculationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ThoroughRecalculationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateBounded<ThoroughRecalculationRequest>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    internal int PendingRequestCount
    {
        get
        {
            lock (_pendingLock)
            {
                return _pendingRequests.Count;
            }
        }
    }

    public bool QueueRecalculation(
        DateOnly startDate,
        DateOnly endDate,
        Guid? selectedGroup,
        Guid? analyseToken,
        IReadOnlyCollection<Guid>? clientIds = null)
    {
        var request = new ThoroughRecalculationRequest(startDate, endDate, selectedGroup, analyseToken, clientIds);

        lock (_pendingLock)
        {
            if (_pendingRequests.Contains(request))
            {
                _logger.LogInformation(
                    "Thorough recalculation for {StartDate} to {EndDate} (group={Group}) is already queued - coalescing",
                    startDate,
                    endDate,
                    selectedGroup);
                return true;
            }

            if (_channel.Writer.TryWrite(request))
            {
                _pendingRequests.Add(request);
                return true;
            }
        }

        _logger.LogWarning(
            "Failed to queue thorough recalculation for {StartDate} to {EndDate} (group={Group}): queue is full",
            startDate,
            endDate,
            selectedGroup);
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ThoroughRecalculationBackgroundService started");

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                lock (_pendingLock)
                {
                    _pendingRequests.Remove(request);
                }

                try
                {
                    await ProcessRequestAsync(request, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing thorough recalculation request for {Start} - {End}",
                        request.StartDate, request.EndDate);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }

        _logger.LogInformation("ThoroughRecalculationBackgroundService stopped");
    }

    private async Task ProcessRequestAsync(ThoroughRecalculationRequest request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var context = sp.GetRequiredService<DataBaseContext>();
        var workMacroService = sp.GetRequiredService<IWorkMacroService>();
        var breakMacroService = sp.GetRequiredService<IBreakMacroService>();
        var periodHoursService = sp.GetRequiredService<IPeriodHoursService>();
        var notificationService = sp.GetRequiredService<IWorkNotificationService>();
        var groupClient = sp.GetRequiredService<IGetAllClientIdsFromGroupAndSubgroups>();

        _logger.LogInformation(
            "Starting thorough recalculation for {Start} - {End} (group={Group}, scenario={Token})",
            request.StartDate,
            request.EndDate,
            request.SelectedGroup,
            request.AnalyseToken);

        List<Guid>? clientIdFilter = request.ClientIds.Count > 0 ? request.ClientIds.ToList() : null;
        if (request.SelectedGroup.HasValue)
        {
            var ids = await groupClient.GetAllClientIdsFromGroupAndSubgroups(request.SelectedGroup.Value);
            var groupClientIds = ids.ToList();
            clientIdFilter = clientIdFilter is null
                ? groupClientIds
                : clientIdFilter.Intersect(groupClientIds).ToList();
        }

        if (clientIdFilter is { Count: 0 })
        {
            _logger.LogWarning(
                "Thorough recalc: client scope (group={Group}, explicit clients={ClientCount}) resolved to no clients - skipping",
                request.SelectedGroup,
                request.ClientIds.Count);
            await SendCompletion(notificationService, request, processedWorks: 0, processedWorkChanges: 0, processedBreaks: 0);
            return;
        }

        var sealedWorkCount = await CountSealedWorksAsync(context, request, clientIdFilter, cancellationToken);
        if (sealedWorkCount > 0)
        {
            _logger.LogWarning(
                "Thorough recalc: skipping {Count} sealed work(s) (LockLevel != None) in {Start}..{End}; their surcharges stay unchanged until they are unsealed and reprocessed",
                sealedWorkCount,
                request.StartDate,
                request.EndDate);
        }

        var works = await LoadWorksAsync(context, request, clientIdFilter, cancellationToken);
        _logger.LogInformation("Thorough recalc: reprocessing {Count} works", works.Count);
        foreach (var work in works)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await workMacroService.ProcessWorkMacroAsync(work);
        }

        var workChanges = await LoadWorkChangesAsync(context, request, clientIdFilter, cancellationToken);
        _logger.LogInformation("Thorough recalc: reprocessing {Count} work changes", workChanges.Count);
        foreach (var workChange in workChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await workMacroService.ProcessWorkChangeMacroAsync(workChange);
        }

        await context.SaveChangesAsync(cancellationToken);

        await breakMacroService.ReprocessAllBreaksAsync(request.StartDate, request.EndDate, clientIdFilter);
        var breakCount = await CountBreaksAsync(context, request, clientIdFilter, cancellationToken);

        if (request.ClientIds.Count > 0 && clientIdFilter is not null)
        {
            foreach (var clientId in clientIdFilter)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await periodHoursService.RecalculatePeriodHoursAsync(
                    clientId,
                    request.StartDate,
                    request.EndDate,
                    request.AnalyseToken);
            }
        }
        else
        {
            await periodHoursService.RecalculateAllClientsAsync(
                request.StartDate,
                request.EndDate,
                request.SelectedGroup,
                request.AnalyseToken);
        }

        _logger.LogInformation(
            "Completed thorough recalculation for {Start} - {End} (works={Works}, workChanges={WorkChanges}, breaks={Breaks})",
            request.StartDate,
            request.EndDate,
            works.Count,
            workChanges.Count,
            breakCount);

        await SendCompletion(notificationService, request, works.Count, workChanges.Count, breakCount);
    }

    private static Task<List<Domain.Models.Schedules.Work>> LoadWorksAsync(
        DataBaseContext context,
        ThoroughRecalculationRequest request,
        List<Guid>? clientIdFilter,
        CancellationToken cancellationToken)
    {
        var query = context.Work
            .Where(w => !w.IsDeleted
                && w.LockLevel == WorkLockLevel.None
                && w.CurrentDate >= request.StartDate
                && w.CurrentDate <= request.EndDate
                && w.AnalyseToken == request.AnalyseToken);

        if (clientIdFilter is { Count: > 0 })
        {
            query = query.Where(w => clientIdFilter.Contains(w.ClientId));
        }

        return query.ToListAsync(cancellationToken);
    }

    private static Task<int> CountSealedWorksAsync(
        DataBaseContext context,
        ThoroughRecalculationRequest request,
        List<Guid>? clientIdFilter,
        CancellationToken cancellationToken)
    {
        var query = context.Work
            .Where(w => !w.IsDeleted
                && w.LockLevel != WorkLockLevel.None
                && w.CurrentDate >= request.StartDate
                && w.CurrentDate <= request.EndDate
                && w.AnalyseToken == request.AnalyseToken);

        if (clientIdFilter is { Count: > 0 })
        {
            query = query.Where(w => clientIdFilter.Contains(w.ClientId));
        }

        return query.CountAsync(cancellationToken);
    }

    private static async Task<List<Domain.Models.Schedules.WorkChange>> LoadWorkChangesAsync(
        DataBaseContext context,
        ThoroughRecalculationRequest request,
        List<Guid>? clientIdFilter,
        CancellationToken cancellationToken)
    {
        var query = context.WorkChange
            .Include(wc => wc.Work)
            .Where(wc => !wc.IsDeleted
                && wc.Work!.LockLevel == WorkLockLevel.None
                && wc.Work.CurrentDate >= request.StartDate
                && wc.Work.CurrentDate <= request.EndDate
                && wc.Work.AnalyseToken == request.AnalyseToken);

        if (clientIdFilter is { Count: > 0 })
        {
            query = query.Where(wc =>
                clientIdFilter.Contains(wc.Work!.ClientId) ||
                (wc.ReplaceClientId.HasValue && clientIdFilter.Contains(wc.ReplaceClientId.Value)));
        }

        return await query.ToListAsync(cancellationToken);
    }

    private static Task<int> CountBreaksAsync(
        DataBaseContext context,
        ThoroughRecalculationRequest request,
        List<Guid>? clientIdFilter,
        CancellationToken cancellationToken)
    {
        var query = context.Break
            .Where(b => !b.IsDeleted
                && b.CurrentDate >= request.StartDate
                && b.CurrentDate <= request.EndDate);

        if (clientIdFilter is { Count: > 0 })
        {
            query = query.Where(b => clientIdFilter.Contains(b.ClientId));
        }

        return query.CountAsync(cancellationToken);
    }

    private static Task SendCompletion(
        IWorkNotificationService notificationService,
        ThoroughRecalculationRequest request,
        int processedWorks,
        int processedWorkChanges,
        int processedBreaks)
    {
        var notification = new ThoroughRecalculationCompletedDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SelectedGroup = request.SelectedGroup,
            AnalyseToken = request.AnalyseToken,
            ProcessedWorks = processedWorks,
            ProcessedWorkChanges = processedWorkChanges,
            ProcessedBreaks = processedBreaks
        };

        return notificationService.NotifyThoroughRecalculationCompleted(notification);
    }

    /// <summary>
    /// Queued recalculation request. ClientIds is normalized to a distinct, sorted array (empty = all
    /// clients) so that value equality and the hash code stay structural despite the collection member:
    /// the synthesized record equality would compare the collection by reference and break the pending
    /// dedupe set, therefore both members are implemented manually over the normalized sequence.
    /// </summary>
    private sealed record ThoroughRecalculationRequest
    {
        public ThoroughRecalculationRequest(
            DateOnly startDate,
            DateOnly endDate,
            Guid? selectedGroup,
            Guid? analyseToken,
            IReadOnlyCollection<Guid>? clientIds)
        {
            StartDate = startDate;
            EndDate = endDate;
            SelectedGroup = selectedGroup;
            AnalyseToken = analyseToken;
            ClientIds = clientIds is { Count: > 0 }
                ? clientIds.Distinct().OrderBy(id => id).ToArray()
                : [];
        }

        public DateOnly StartDate { get; }
        public DateOnly EndDate { get; }
        public Guid? SelectedGroup { get; }
        public Guid? AnalyseToken { get; }
        public IReadOnlyList<Guid> ClientIds { get; }

        public bool Equals(ThoroughRecalculationRequest? other)
        {
            return other is not null
                && StartDate == other.StartDate
                && EndDate == other.EndDate
                && SelectedGroup == other.SelectedGroup
                && AnalyseToken == other.AnalyseToken
                && ClientIds.SequenceEqual(other.ClientIds);
        }

        public override int GetHashCode()
        {
            var hash = default(HashCode);
            hash.Add(StartDate);
            hash.Add(EndDate);
            hash.Add(SelectedGroup);
            hash.Add(AnalyseToken);
            foreach (var clientId in ClientIds)
            {
                hash.Add(clientId);
            }

            return hash.ToHashCode();
        }
    }
}
