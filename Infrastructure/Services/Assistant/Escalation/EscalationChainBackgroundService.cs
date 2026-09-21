// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic sweep for the escalation chain (docs/ENTWURF-eskalationskette-2026-08-16.md §5). Force-exhausts
/// any Running chain past its own DeadlineUtc FIRST, then expires stages whose DueAtUtc has passed and
/// hands each expiry to IEscalationChainService.AdvanceAsync for the next wave, and finally checks F3
/// (a chain's referenced Break got soft-deleted -> Superseded). The order is part of the contract: a
/// chain that drifted past its deadline - a stalled sweep, a long restart - would otherwise have its
/// expiry reach AdvanceAsync while still Running, and EscalationWaveCalculator answers a non-positive
/// remaining budget with a PARALLEL wave, so one stale chain would wake every roster member left on it
/// at once. Exhausting first leaves AdvanceAsync nothing to notify - and since an exhaust now cancels the
/// chain's remaining Pending/Notified stages in its own transaction, a stale chain's stages are already
/// terminal by the time the expiry step runs, so that step finds nothing due for them at all. The
/// force-exhaust goes through IEscalationChainService rather than the repository so the inbox rows of the
/// stages it cancels are closed as well. Enabled by default via
/// BackgroundServiceOptions.EscalationChain because the proactive approval chain depends on it - see
/// that flag's XML doc for why, unlike most services in this folder, it is meant to run on every instance.
/// </summary>
/// <param name="serviceProvider">Creates a scoped DI provider per sweep cycle.</param>
/// <param name="timeProvider">Injected clock; RunCycleAsync takes "now" from here so a test can drive
/// the 03:00/03:20/03:26 reference case with a fake clock instead of waiting on it.</param>
/// <param name="options">Feature flag and cadence for this service.</param>
/// <param name="logger">Structured log per cycle.</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Assistant.Escalation;

public class EscalationChainBackgroundService : BackgroundService
{
    internal const string OverdueOutcomeReason = "deadline passed before every stage could be resolved";
    private const string SupersededOutcomeReason = "the referenced absence report was cancelled";

    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<EscalationChainBackgroundService> _logger;

    public EscalationChainBackgroundService(
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<EscalationChainBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EscalationChain)
        {
            _logger.LogInformation("EscalationChainBackgroundService is disabled via configuration");
            return;
        }

        var startupDelay = TimeSpan.FromSeconds(_options.EscalationChainStartupDelaySeconds);
        _logger.LogInformation(
            "EscalationChainBackgroundService started, first run in {Delay}s", startupDelay.TotalSeconds);

        try
        {
            await Task.Delay(startupDelay, _timeProvider, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("EscalationChainBackgroundService cancelled during startup delay; exiting cleanly.");
            return;
        }

        await RunCycleAsync(stoppingToken);

        var interval = TimeSpan.FromSeconds(_options.EscalationChainSweepIntervalSeconds);
        using var timer = new PeriodicTimer(interval, _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCycleAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("EscalationChainBackgroundService stopped");
    }

    internal async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IEscalationChainRepository>();
            var chainService = scope.ServiceProvider.GetRequiredService<IEscalationChainService>();

            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            var overdueCount = await ExhaustOverdueChainsAsync(repository, chainService, nowUtc, cancellationToken);
            var expiredCount = await ExpireDueStagesAsync(repository, chainService, nowUtc, cancellationToken);
            var supersededCount = await SupersedeCancelledReportsAsync(repository, cancellationToken);

            _logger.LogInformation(
                "EscalationChainBackgroundService - sweep completed in {Ms}ms: {Expired} stage(s) expired, {Overdue} chain(s) force-exhausted, {Superseded} chain(s) superseded",
                sw.ElapsedMilliseconds, expiredCount, overdueCount, supersededCount);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "EscalationChainBackgroundService - sweep failed after {Ms}ms", sw.ElapsedMilliseconds);
        }
    }

    private static async Task<int> ExpireDueStagesAsync(
        IEscalationChainRepository repository, IEscalationChainService chainService, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var dueStages = await repository.GetDueStagesAsync(nowUtc, cancellationToken);
        var expired = 0;

        foreach (var stage in dueStages)
        {
            if (!await repository.TryExpireStageAsync(stage.Id, cancellationToken))
            {
                continue;
            }

            expired++;
            await chainService.AdvanceAsync(stage.EscalationChainId, cancellationToken);
        }

        return expired;
    }

    private static async Task<int> ExhaustOverdueChainsAsync(
        IEscalationChainRepository repository,
        IEscalationChainService chainService,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var overdueChainIds = await repository.GetOverdueRunningChainIdsAsync(nowUtc, cancellationToken);
        var exhausted = 0;

        foreach (var chainId in overdueChainIds)
        {
            if (await chainService.ForceExhaustAsync(chainId, OverdueOutcomeReason, cancellationToken))
            {
                exhausted++;
            }
        }

        return exhausted;
    }

    private static async Task<int> SupersedeCancelledReportsAsync(
        IEscalationChainRepository repository, CancellationToken cancellationToken)
    {
        var candidates = await repository.GetRunningChainsWithAbsenceBreakAsync(cancellationToken);
        var superseded = 0;

        foreach (var chain in candidates)
        {
            if (chain.AbsenceBreakId is not { } breakId
                || !await repository.IsBreakDeletedAsync(breakId, cancellationToken))
            {
                continue;
            }

            if (!await repository.TrySupersedeChainAsync(chain.Id, SupersededOutcomeReason, cancellationToken))
            {
                continue;
            }

            await repository.CancelRemainingStagesAsync(chain.Id, Guid.Empty, cancellationToken);
            superseded++;
        }

        return superseded;
    }
}
