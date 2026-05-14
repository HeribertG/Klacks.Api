// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background service that performs a thorough recalculation of schedule entries in a date range.
/// Reprocesses Work, WorkChange and Break macros, then recalculates all client period hours.
/// Notifies clients via SignalR when finished.
/// </summary>
/// <param name="serviceProvider">Root service provider for scoped resolution per work item</param>
/// <param name="logger">Logger for diagnostics</param>

using System.Threading.Channels;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services;

public class ThoroughRecalculationBackgroundService : BackgroundService
{
    private const int ChannelCapacity = 20;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ThoroughRecalculationBackgroundService> _logger;
    private readonly Channel<ThoroughRecalculationRequest> _channel;

    public ThoroughRecalculationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ThoroughRecalculationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateBounded<ThoroughRecalculationRequest>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
    }

    public bool QueueRecalculation(
        DateOnly startDate,
        DateOnly endDate,
        Guid? selectedGroup,
        Guid? analyseToken)
    {
        var request = new ThoroughRecalculationRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            SelectedGroup = selectedGroup,
            AnalyseToken = analyseToken
        };

        if (_channel.Writer.TryWrite(request))
        {
            return true;
        }

        _logger.LogWarning(
            "Failed to queue thorough recalculation for {StartDate} to {EndDate} (group={Group})",
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

        List<Guid>? clientIdFilter = null;
        if (request.SelectedGroup.HasValue)
        {
            var ids = await groupClient.GetAllClientIdsFromGroupAndSubgroups(request.SelectedGroup.Value);
            clientIdFilter = ids.ToList();
            if (clientIdFilter.Count == 0)
            {
                _logger.LogWarning(
                    "Thorough recalc: group {Group} resolved to no clients - skipping",
                    request.SelectedGroup);
                await SendCompletion(notificationService, request, processedWorks: 0, processedWorkChanges: 0, processedBreaks: 0);
                return;
            }
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

        await periodHoursService.RecalculateAllClientsAsync(
            request.StartDate,
            request.EndDate,
            request.SelectedGroup,
            request.AnalyseToken);

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
                && w.CurrentDate >= request.StartDate
                && w.CurrentDate <= request.EndDate
                && w.AnalyseToken == request.AnalyseToken);

        if (clientIdFilter is { Count: > 0 })
        {
            query = query.Where(w => clientIdFilter.Contains(w.ClientId));
        }

        return query.ToListAsync(cancellationToken);
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
                && wc.Work!.CurrentDate >= request.StartDate
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

    private sealed record ThoroughRecalculationRequest
    {
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public Guid? SelectedGroup { get; init; }
        public Guid? AnalyseToken { get; init; }
    }
}
