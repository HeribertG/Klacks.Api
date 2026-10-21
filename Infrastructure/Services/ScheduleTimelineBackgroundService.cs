// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Background worker for schedule validation: collisions, rest periods, working hours, travel times.
/// Receives check requests via Channel and sends results via SignalR. Owns the queue, the drain/dedupe loop and
/// the transient-failure retry; the checks themselves are ScheduleTimelineSingleCheck and ScheduleTimelineRangeCheck,
/// resolved from a fresh scope on every attempt of a job.
/// </summary>
/// <param name="serviceProvider">Root provider the per-attempt scopes are created from</param>
using System.Threading.Channels;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Services.ScheduleTimeline;

namespace Klacks.Api.Infrastructure.Services;

public class ScheduleTimelineBackgroundService : BackgroundService, IScheduleTimelineService
{
    private const int ChannelCapacity = 500;
    private const string NoTokenLabel = "null";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduleTimelineBackgroundService> _logger;
    private readonly Channel<TimelineCheckRequest> _channel;

    public ScheduleTimelineBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ScheduleTimelineBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _channel = Channel.CreateBounded<TimelineCheckRequest>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
    }

    public void QueueCheck(Guid clientId, DateOnly date, Guid? analyseToken)
    {
        _logger.LogDebug("[COLLISION-TRACE] QueueCheck queued Client={ClientId} Date={Date} token={Token}",
            clientId, date, TokenLabel(analyseToken));

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
                clientId, date, TokenLabel(analyseToken));
        }
    }

    public void QueueRangeCheck(DateOnly startDate, DateOnly endDate, Guid? analyseToken)
    {
        _logger.LogDebug("[COLLISION-TRACE] QueueRangeCheck queued {Start} - {End} token={Token}",
            startDate, endDate, TokenLabel(analyseToken));

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
                startDate, endDate, TokenLabel(analyseToken));
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduleTimelineBackgroundService started");

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                foreach (var job in DrainDeduplicated(request))
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

    /// <summary>
    /// Drains the channel and deduplicates by semantic key. Different semantic keys (e.g. SingleCheck Client
    /// A/Date X and RangeCheck 1.-14. April) MUST both run; only requests that share the exact same key collapse
    /// (latest wins).
    /// </summary>
    private IEnumerable<TimelineCheckRequest> DrainDeduplicated(TimelineCheckRequest first)
    {
        var pending = new Dictionary<string, TimelineCheckRequest>(StringComparer.Ordinal)
        {
            [BuildRequestKey(first)] = first
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

        return pending.Values;
    }

    private static string BuildRequestKey(TimelineCheckRequest r)
        => r.IsRangeCheck
            ? $"range:{r.StartDate:O}:{r.EndDate:O}:{TokenLabel(r.AnalyseToken)}"
            : $"single:{r.ClientId}:{r.Date:O}:{TokenLabel(r.AnalyseToken)}";

    private static string TokenLabel(Guid? analyseToken) => analyseToken?.ToString() ?? NoTokenLabel;

    private async Task ProcessJobWithRetryAsync(TimelineCheckRequest job, CancellationToken stoppingToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                await RunAttemptAsync(job, stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < TimelineCheckTransientFailures.MaxRetries && TimelineCheckTransientFailures.IsTransient(ex))
            {
                attempt++;
                var delay = TimelineCheckTransientFailures.NextRetryDelay();
                _logger.LogWarning(ex,
                    "[COLLISION-TRACE] Transient failure on {Key} (attempt {Attempt}/{Max}), retrying in {Delay}ms",
                    BuildRequestKey(job), attempt, TimelineCheckTransientFailures.MaxRetries, (int)delay.TotalMilliseconds);

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
                LogJobFailure(job, ex, attempt + 1);
                return;
            }
        }
    }

    private async Task RunAttemptAsync(TimelineCheckRequest job, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();

        if (job.IsRangeCheck)
        {
            var rangeCheck = scope.ServiceProvider.GetRequiredService<ScheduleTimelineRangeCheck>();
            _logger.LogDebug("[COLLISION-TRACE] START RangeCheck {Start} - {End} token={Token}",
                job.StartDate, job.EndDate, TokenLabel(job.AnalyseToken));
            await rangeCheck.RunAsync(job.StartDate, job.EndDate, job.AnalyseToken, stoppingToken);
            _logger.LogDebug("[COLLISION-TRACE] DONE RangeCheck {Start} - {End} token={Token}",
                job.StartDate, job.EndDate, TokenLabel(job.AnalyseToken));
        }
        else
        {
            var singleCheck = scope.ServiceProvider.GetRequiredService<ScheduleTimelineSingleCheck>();
            _logger.LogDebug("[COLLISION-TRACE] START SingleCheck Client={ClientId} Date={Date} token={Token}",
                job.ClientId, job.Date, TokenLabel(job.AnalyseToken));
            await singleCheck.RunAsync(job.ClientId, job.Date, job.AnalyseToken, stoppingToken);
            _logger.LogDebug("[COLLISION-TRACE] DONE SingleCheck Client={ClientId} token={Token}",
                job.ClientId, TokenLabel(job.AnalyseToken));
        }
    }

    private void LogJobFailure(TimelineCheckRequest job, Exception ex, int attempts)
    {
        if (job.IsRangeCheck)
        {
            _logger.LogError(ex, "[COLLISION-TRACE] FAILED RangeCheck {Start} - {End} token={Token} after {Attempts} attempt(s): {Message}",
                job.StartDate, job.EndDate, TokenLabel(job.AnalyseToken), attempts, ex.Message);
        }
        else
        {
            _logger.LogError(ex, "[COLLISION-TRACE] FAILED SingleCheck Client={ClientId} Date={Date} token={Token} after {Attempts} attempt(s): {Message}",
                job.ClientId, job.Date, TokenLabel(job.AnalyseToken), attempts, ex.Message);
        }
    }
}
