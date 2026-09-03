// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background worker for seal_open_orders batches too large to run inside a chat turn (see
/// SealOpenOrdersSkill.SealOpenOrdersSynchronousLimit). Callers enqueue a SealOpenOrdersJob and each is
/// run in its own DI scope, exactly like GroupGeocodingBackgroundService: a fresh IMediator resolves the
/// same SealOpenOrdersCommandHandler the synchronous path uses, so the sealing itself has exactly one
/// code path regardless of size. The job carries the acting user's id explicitly — there is no
/// HttpContext on a background thread, so DataBaseContext.OnBeforeSaving falls back to "Anonymous" for
/// the audit columns of rows the job writes, same as every other background writer in this codebase
/// (SlackOwnerBridgeService, ErpOrderImportRunner, ...); the inbox notification below is what actually
/// reaches the right person. A failure that escapes IMediator.Send (the initial order query, the
/// auto-assign step) aborts the whole run; per-order sealing failures do not, because the handler
/// isolates each order in its own transaction and reports them inside the result instead of throwing.
/// The queue itself is an in-memory Channel with no persistence: a process restart while a job is queued
/// or mid-run silently loses it, exactly like GroupGeocodingBackgroundService's queue, and for the same
/// reason a full recovery scan is not implemented here — unlike a group missing coordinates, a lost seal
/// job leaves no marker to resume from (SealOpenOrdersCommand carries no persisted identity until it
/// completes) and the caller already knows to check via seal_open_orders(apply=false) if the promised
/// inbox message never arrives.
/// </summary>

using System.Diagnostics;
using System.Threading.Channels;
using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Infrastructure.Services.Orders;

public class SealOpenOrdersJobBackgroundService : BackgroundService, ISealOpenOrdersJobQueue
{
    private const int ChannelCapacity = 20;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SealOpenOrdersJobBackgroundService> _logger;
    private readonly Channel<SealOpenOrdersJob> _channel;

    public SealOpenOrdersJobBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SealOpenOrdersJobBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateBounded<SealOpenOrdersJob>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    public bool Enqueue(SealOpenOrdersJob job)
    {
        if (_channel.Writer.TryWrite(job))
        {
            return true;
        }

        _logger.LogWarning("Failed to queue seal_open_orders job {JobId} for user {UserId}: queue is full", job.JobId, job.UserId);
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SealOpenOrdersJobBackgroundService started");

        try
        {
            await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessJobAsync(job, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown - ignore
        }

        _logger.LogInformation("SealOpenOrdersJobBackgroundService stopped");
    }

    private async Task ProcessJobAsync(SealOpenOrdersJob job, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(job.Command, stoppingToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "seal_open_orders job {JobId} finished: {Sealed} sealed, {Blocked} blocked, {Failed} failed of {Total} in {Elapsed}",
                job.JobId, result.SealedCount, result.BlockedCount, result.FailedCount, result.TotalOrders, stopwatch.Elapsed);

            await NotifyCompletedAsync(job, result, stopwatch.Elapsed, stoppingToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "seal_open_orders job {JobId} aborted after {Elapsed}", job.JobId, stopwatch.Elapsed);

            await NotifyFailedAsync(job, ex, stoppingToken);
        }
    }

    private async Task NotifyCompletedAsync(
        SealOpenOrdersJob job, SealOpenOrdersResult result, TimeSpan elapsed, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var triggerService = scope.ServiceProvider.GetRequiredService<IAgentTriggerService>();
            var failureSample = result.Failures
                .Select(f => (f.OrderName, f.Reason))
                .ToList();

            await triggerService.OnEventAsync(
                new BulkSealOrdersCompletedTriggerEvent(
                    job.JobId,
                    job.UserId,
                    result.TotalOrders,
                    result.SealedCount,
                    result.BlockedCount,
                    result.FailedCount,
                    elapsed,
                    failureSample),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "seal_open_orders job {JobId} finished but the inbox notification failed", job.JobId);
        }
    }

    private async Task NotifyFailedAsync(SealOpenOrdersJob job, Exception failure, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var triggerService = scope.ServiceProvider.GetRequiredService<IAgentTriggerService>();

            await triggerService.OnEventAsync(
                new BulkSealOrdersFailedTriggerEvent(job.JobId, job.UserId, failure.Message),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "seal_open_orders job {JobId} aborted and the failure inbox notification ALSO failed", job.JobId);
        }
    }
}
