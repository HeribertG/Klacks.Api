// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Services;

/// <summary>
/// Singleton that runs the knowledge index synchronization in the background, one run at a time.
/// Requests that arrive while a run executes only set a dirty flag, so any number of them collapse
/// into a single follow-up run. Each run gets a fresh DI scope and the application's stopping token;
/// a failed run is logged and recorded in the status, never retried automatically, and never keeps
/// the next request from running.
/// </summary>
/// <param name="scopeFactory">Creates the per-run scope that resolves the scoped synchronizer.</param>
/// <param name="lifetime">Supplies ApplicationStopping, which cancels a running synchronization.</param>
/// <param name="timeProvider">Clock for the completion and failure timestamps in the status.</param>
/// <param name="logger">Reports run start, cancellation and failure.</param>
public sealed class KnowledgeIndexSyncScheduler : IKnowledgeIndexSyncScheduler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<KnowledgeIndexSyncScheduler> _logger;

    private readonly object _sync = new();
    private readonly List<(long Run, TaskCompletionSource Completion)> _waiters = [];

    private bool _draining;
    private bool _pending;
    private int _pendingRequests;
    private string? _pendingReason;
    private bool _isRunning;
    private long _startedRuns;
    private DateTimeOffset? _lastCompletedUtc;
    private DateTimeOffset? _lastFailedUtc;
    private string? _lastReason;
    private string? _lastError;

    public KnowledgeIndexSyncScheduler(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        TimeProvider timeProvider,
        ILogger<KnowledgeIndexSyncScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _lifetime = lifetime;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public KnowledgeIndexSyncStatus Status
    {
        get
        {
            lock (_sync)
            {
                return new KnowledgeIndexSyncStatus(
                    _isRunning, _pending, _lastCompletedUtc, _lastFailedUtc, _lastReason, _lastError);
            }
        }
    }

    public void Request(string reason)
    {
        lock (_sync)
        {
            MarkPendingUnderLock(reason);
        }
    }

    public Task RunNowAsync(string reason, CancellationToken cancellationToken)
    {
        Task completion;
        lock (_sync)
        {
            // The pending flag set below can only be consumed by the next run to start, so that run -
            // not the one possibly in flight, which may have read the catalogue before the caller's
            // change - is the one this caller waits for.
            var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((_startedRuns + 1, completionSource));
            completion = completionSource.Task;
            MarkPendingUnderLock(reason);
        }

        return completion.WaitAsync(cancellationToken);
    }

    private void MarkPendingUnderLock(string reason)
    {
        _pending = true;
        _pendingReason = reason;
        _pendingRequests++;

        if (_draining)
        {
            return;
        }

        _draining = true;

        // Callers are HTTP requests; without suppressing the flow the run would inherit their
        // AsyncLocal state (HttpContext, logging scopes) and outlive the request it belongs to.
        // SuppressFlow throws when the flow is already suppressed, hence the check.
        if (ExecutionContext.IsFlowSuppressed())
        {
            _ = Task.Run(DrainAsync);
            return;
        }

        using (ExecutionContext.SuppressFlow())
        {
            _ = Task.Run(DrainAsync);
        }
    }

    private async Task DrainAsync()
    {
        try
        {
            while (TryBeginRun(out var run, out var reason, out var coalescedRequests))
            {
                try
                {
                    await RunOnceAsync(reason, coalescedRequests);
                }
                finally
                {
                    EndRun(run);
                }
            }
        }
        catch (Exception ex)
        {
            // RunOnceAsync handles every failure of the synchronization itself; landing here means the
            // bookkeeping failed. The gate must reopen regardless, or no request would ever run again.
            _logger.LogError(ex, "Knowledge index sync scheduler failed outside a run; the gate is reopened.");
            lock (_sync)
            {
                _isRunning = false;
                _draining = false;
                ReleaseWaitersUnderLock(long.MaxValue);
            }
        }
    }

    private bool TryBeginRun(out long run, out string reason, out int coalescedRequests)
    {
        lock (_sync)
        {
            if (!_pending || _lifetime.ApplicationStopping.IsCancellationRequested)
            {
                _draining = false;

                if (_lifetime.ApplicationStopping.IsCancellationRequested)
                {
                    // Nothing will ever run again in this process, so a pending flag would only make
                    // the status claim work that is not going to happen.
                    _pending = false;
                    _pendingReason = null;
                    _pendingRequests = 0;
                    ReleaseWaitersUnderLock(long.MaxValue);
                }

                run = 0;
                reason = string.Empty;
                coalescedRequests = 0;
                return false;
            }

            _pending = false;
            reason = _pendingReason ?? string.Empty;
            coalescedRequests = _pendingRequests;
            _pendingReason = null;
            _pendingRequests = 0;
            _isRunning = true;
            _lastReason = reason;
            run = ++_startedRuns;
            return true;
        }
    }

    private void EndRun(long run)
    {
        lock (_sync)
        {
            _isRunning = false;
            ReleaseWaitersUnderLock(run);
        }
    }

    private void ReleaseWaitersUnderLock(long completedRun)
    {
        for (var i = _waiters.Count - 1; i >= 0; i--)
        {
            if (_waiters[i].Run <= completedRun)
            {
                _waiters[i].Completion.TrySetResult();
                _waiters.RemoveAt(i);
            }
        }
    }

    private async Task RunOnceAsync(string reason, int coalescedRequests)
    {
        var stoppingToken = _lifetime.ApplicationStopping;

        try
        {
            _logger.LogInformation(
                "Knowledge index sync started after {Reason} ({Requests} request(s) coalesced into this run).",
                reason,
                coalescedRequests);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<IKnowledgeIndexSynchronizer>();
            await synchronizer.SyncAsync(stoppingToken);

            lock (_sync)
            {
                _lastCompletedUtc = _timeProvider.GetUtcNow();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Knowledge index sync after {Reason} was cancelled because the application is stopping.",
                reason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Knowledge index sync after {Reason} failed; retrieval serves the previous index until the next successful sync.",
                reason);

            lock (_sync)
            {
                _lastFailedUtc = _timeProvider.GetUtcNow();
                _lastError = ex.Message;
            }
        }
    }
}
