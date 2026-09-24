// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic sweep of the inbound clarification dialog: every open clarification whose answer deadline
/// has passed is moved to Expired with a conditional transition (only from Open, so an answer arriving
/// in the same moment or a second instance wins or loses cleanly) and the planners are told that the
/// question went unanswered, with the original message and the affected shift. The first cycle runs
/// right after a short startup delay and catches up every deadline missed while the host was down.
/// Each cycle has a second, independent retention step: the raw original text of rounds that ended longer
/// ago than the configured retention (default 30 days) is cleared - counted from resolved_at for closed
/// rounds and from asked_at for Suggested ones - only the count is logged, and a failure of either step
/// never stops the other. The retention is coupled to the sweep flag: it is a step of this service, not a
/// service of its own, so BackgroundServices:InboundClarificationSweep=false switches off the expiry AND the
/// retention, and the original message text of ended rounds is then kept indefinitely.
/// Resolves the repository, the notifier, the text service and ICompanyClock from a fresh scope per cycle (ICompanyClock is
/// scoped and must not be captured by a hosted service). A failing notification is logged and does not
/// stop the remaining rows; a failing cycle is logged and never escapes. A cancellation of the stopping
/// token is rethrown and ends the service. The planner notice is at most once: a transition that won but
/// whose notification failed is not retried, because only Open rows are swept. Registered only when
/// BackgroundServices:InboundClarificationSweep is on.
/// </summary>
/// <param name="serviceProvider">Creates the scope of each cycle</param>
/// <param name="timeProvider">Injected clock, so tests can drive "now"</param>
/// <param name="options">Flag, cadence, startup delay and original-text retention; the durations are clamped to MinSweepSeconds..MaxSweepSeconds and the retention to MinOriginalTextRetentionDays..MaxOriginalTextRetentionDays, so a zero, negative or oversized configured value cannot crash the service or wipe fresh data</param>
/// <param name="logger">Lifecycle and per-cycle log</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Constants;
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
            await Task.Delay(ClampedSeconds(_options.InboundClarificationSweepStartupDelaySeconds), _timeProvider, stoppingToken);
            await RunCycleAsync(stoppingToken);

            using var timer = new PeriodicTimer(ClampedSeconds(_options.InboundClarificationSweepIntervalSeconds), _timeProvider);
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

    internal static TimeSpan ClampedSeconds(int configuredSeconds) =>
        TimeSpan.FromSeconds(Math.Clamp(
            configuredSeconds,
            InboundClarificationConstants.MinSweepSeconds,
            InboundClarificationConstants.MaxSweepSeconds));

    internal static int ClampedRetentionDays(int configuredDays) =>
        Math.Clamp(
            configuredDays,
            InboundClarificationConstants.MinOriginalTextRetentionDays,
            InboundClarificationConstants.MaxOriginalTextRetentionDays);

    internal async Task<int> RunCycleAsync(CancellationToken cancellationToken)
    {
        var expired = await ExpireDueAsync(cancellationToken);
        await ClearRetainedTextsAsync(cancellationToken);
        return expired;
    }

    private async Task<int> ExpireDueAsync(CancellationToken cancellationToken)
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
            var textService = scope.ServiceProvider.GetRequiredService<IClarificationTextService>();
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
                await NotifySafelyAsync(notifier, textService, clarification, companyTimeZone, cancellationToken);
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

    private async Task ClearRetainedTextsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInboundClarificationRepository>();
            var retentionDays = ClampedRetentionDays(_options.InboundClarificationOriginalTextRetentionDays);
            var cutoffUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-retentionDays);

            var cleared = await repository.ClearOriginalTextAsync(cutoffUtc, cancellationToken);
            if (cleared > 0)
            {
                _logger.LogInformation(
                    "ClarificationExpirySweep cleared the original text of {Count} ended clarification(s)", cleared);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClarificationExpirySweep retention step failed");
        }
    }

    private async Task NotifySafelyAsync(
        IInboundAnalysisNotifier notifier,
        IClarificationTextService textService,
        InboundClarification clarification,
        TimeZoneInfo companyTimeZone,
        CancellationToken cancellationToken)
    {
        try
        {
            await notifier.NotifyMessageAsync(
                await textService.ExpiredAsync(
                    clarification.SenderDisplay,
                    clarification.Question,
                    ClarificationTimeConversion.ToLocal(clarification.AskedAt, companyTimeZone),
                    ClarificationTimeConversion.ToLocal(clarification.DeadlineAt, companyTimeZone),
                    clarification.OriginalText,
                    clarification.ShiftContext,
                    cancellationToken),
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
