// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Fallback trigger for the preference-learner capture. Most captures are measured the moment their period is
/// sealed; this daily timer catches the rest — a run whose period ended but was never sealed within the grace
/// window is measured once and stamped Expired, so the capture never stays unmeasured forever. Inert per tick
/// when nothing is overdue (a single query, no writes). Each tick runs in its own DI scope with cooperative
/// cancellation; a failure is logged and the next tick retries.
/// </summary>
public sealed class WizardRunCaptureMeasurementBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan GracePeriod = TimeSpan.FromDays(14);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WizardRunCaptureMeasurementBackgroundService> _logger;

    public WizardRunCaptureMeasurementBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WizardRunCaptureMeasurementBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "WizardRunCapture measurement fallback enabled; tick every {Days}d, grace {Grace}d.",
            TickInterval.TotalDays, GracePeriod.TotalDays);
        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await MeasureExpiredAsync(scope, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WizardRunCapture measurement fallback tick failed");
            }
        }
    }

    private async Task MeasureExpiredAsync(IServiceScope scope, CancellationToken ct)
    {
        var repository = scope.ServiceProvider.GetRequiredService<IWizardRunCaptureRepository>();
        var measurementService = scope.ServiceProvider.GetRequiredService<IWizardRunCaptureMeasurementService>();

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-(int)GracePeriod.TotalDays);
        var expired = await repository.GetUnmeasuredExpiredAsync(cutoff, ct);
        if (expired.Count == 0)
        {
            return;
        }

        var measured = 0;
        foreach (var capture in expired)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await measurementService.MeasureAsync(capture, CaptureOutcome.Expired, ct);
                measured++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "Fallback measurement failed for WizardRunCapture {CaptureId}", capture.Id);
            }
        }

        _logger.LogInformation(
            "WizardRunCapture measurement fallback measured {Measured}/{Total} expired capture(s) as Expired.",
            measured, expired.Count);
    }
}
