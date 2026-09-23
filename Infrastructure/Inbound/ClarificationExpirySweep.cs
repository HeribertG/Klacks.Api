// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic sweep of the inbound clarification dialog: every open clarification whose answer deadline
/// has passed is moved to Expired with a conditional transition (only from Open, so an answer arriving
/// in the same moment or a second instance wins or loses cleanly) and the planners are told that the
/// question went unanswered, with the original message and the affected shift. The first cycle runs
/// right after a short startup delay and catches up every deadline missed while the host was down.
/// Resolves the repository, the notifier and ICompanyClock from a fresh scope per cycle (ICompanyClock is
/// scoped and must not be captured by a hosted service). A failing notification is logged and does not
/// stop the remaining rows; a failing cycle is logged and never escapes. A cancellation of the stopping
/// token is rethrown and ends the service. The planner notice is at most once: a transition that won but
/// whose notification failed is not retried, because only Open rows are swept. Registered only when
/// BackgroundServices:InboundClarificationSweep is on.
/// </summary>
/// <param name="serviceProvider">Creates the scope of each cycle</param>
/// <param name="timeProvider">Injected clock, so tests can drive "now"</param>
/// <param name="options">Flag, cadence and startup delay</param>
/// <param name="logger">Lifecycle and per-cycle log</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Inbound;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Inbound;

public sealed class ClarificationExpirySweep : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<ClarificationExpirySweep> _logger;

    public ClarificationExpirySweep(
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<ClarificationExpirySweep> logger)
    {
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.InboundClarificationSweep)
        {
            _logger.LogInformation("ClarificationExpirySweep is disabled via configuration");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.InboundClarificationSweepStartupDelaySeconds), _timeProvider, stoppingToken);
            await RunCycleAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.InboundClarificationSweepIntervalSeconds), _timeProvider);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCycleAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("ClarificationExpirySweep stopped");
    }

    internal async Task<int> RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInboundClarificationRepository>();
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            var due = await repository.GetOpenDueAsync(nowUtc, cancellationToken);
            if (due.Count == 0)
            {
                return 0;
            }

            var notifier = scope.ServiceProvider.GetRequiredService<IInboundAnalysisNotifier>();
            var companyTimeZone = await scope.ServiceProvider.GetRequiredService<ICompanyClock>().GetTimeZoneAsync(cancellationToken);
            var expired = 0;

            foreach (var clarification in due)
            {
                var won = await repository.TryResolveAsync(
                    clarification.Id,
                    InboundClarificationStatus.Expired,
                    answerSourceId: null,
                    resultAnalysisId: null,
                    resolvedAtUtc: nowUtc,
                    cancellationToken);
                if (!won)
                {
                    continue;
                }

                expired++;
                await NotifySafelyAsync(notifier, clarification, companyTimeZone, cancellationToken);
            }

            _logger.LogInformation("ClarificationExpirySweep expired {Count} clarification(s)", expired);
            return expired;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClarificationExpirySweep cycle failed");
            return 0;
        }
    }

    private async Task NotifySafelyAsync(
        IInboundAnalysisNotifier notifier, InboundClarification clarification, TimeZoneInfo companyTimeZone, CancellationToken cancellationToken)
    {
        try
        {
            await notifier.NotifyMessageAsync(
                ClarificationNotificationTexts.Expired(
                    clarification.SenderDisplay,
                    clarification.Question,
                    ClarificationTimeConversion.ToLocal(clarification.AskedAt, companyTimeZone),
                    ClarificationTimeConversion.ToLocal(clarification.DeadlineAt, companyTimeZone),
                    clarification.OriginalText,
                    clarification.ShiftContext),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Planners could not be told that clarification {ClarificationId} expired", clarification.Id);
        }
    }

}
