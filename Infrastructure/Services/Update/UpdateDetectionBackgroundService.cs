// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodically checks the configured channel's update manifest and logs whether a newer version is
/// available. Detection only — it never writes execution (Pending) rows; enqueuing an update is the
/// job of an explicit trigger (admin action or the auto-update policy), added in a later phase.
/// </summary>
/// <param name="scopeFactory">Factory for creating DI scopes in background tasks</param>
/// <param name="logger">Logger for diagnostic output</param>
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Update;

public class UpdateDetectionBackgroundService : BackgroundService
{
    private const int DefaultIntervalHours = 6;
    private const int MinIntervalHours = 1;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DisabledPollInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UpdateDetectionBackgroundService> _logger;

    public UpdateDetectionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<UpdateDetectionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Update detection background service started");

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var nextInterval = DisabledPollInterval;
                try
                {
                    nextInterval = await RunDetectionCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in update detection background service");
                }

                await Task.Delay(nextInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Update detection background service stopped");
    }

    private async Task<TimeSpan> RunDetectionCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsReader = scope.ServiceProvider.GetRequiredService<ISettingsReader>();

        var autoSetting = await settingsReader.GetSetting(SettingsConstants.UPDATE_AUTO_ENABLED);
        var autoEnabled = bool.TryParse(autoSetting?.Value, out var enabled) && enabled;
        if (!autoEnabled)
        {
            return DisabledPollInterval;
        }

        var intervalSetting = await settingsReader.GetSetting(SettingsConstants.UPDATE_CHECK_INTERVAL_HOURS);
        var intervalHours = int.TryParse(intervalSetting?.Value, out var hours) ? Math.Max(MinIntervalHours, hours) : DefaultIntervalHours;

        var channelSetting = await settingsReader.GetSetting(SettingsConstants.UPDATE_CHANNEL);
        var channel = Enum.TryParse<UpdateChannel>(channelSetting?.Value, ignoreCase: true, out var c) ? c : UpdateChannel.Stable;

        var reader = scope.ServiceProvider.GetRequiredService<IUpdateManifestReader>();
        var manifest = await reader.GetManifestAsync(channel, stoppingToken);
        if (manifest is null)
        {
            _logger.LogInformation("Update detection: no manifest available for channel {Channel}.", channel);
            return TimeSpan.FromHours(intervalHours);
        }

        var evaluator = scope.ServiceProvider.GetRequiredService<IUpdateAvailabilityEvaluator>();
        var current = new SemanticVersion(
            Klacks.Api.Application.Constants.MyVersion.Major,
            Klacks.Api.Application.Constants.MyVersion.Minor,
            Klacks.Api.Application.Constants.MyVersion.Patch);

        var availability = evaluator.Evaluate(current, manifest);
        if (!availability.IsUpdateAvailable)
        {
            _logger.LogInformation("Update detection: up to date on channel {Channel} (version {Current}).", channel, availability.CurrentVersion);
            return TimeSpan.FromHours(intervalHours);
        }

        _logger.LogInformation(
            "Update detection: version {Latest} available on channel {Channel} (current {Current}, status {Status}).",
            availability.LatestVersion, channel, availability.CurrentVersion, availability.Status);

        await TryAutoTriggerAsync(scope, settingsReader, availability, stoppingToken);

        return TimeSpan.FromHours(intervalHours);
    }

    private async Task TryAutoTriggerAsync(
        IServiceScope scope,
        ISettingsReader settingsReader,
        UpdateAvailability availability,
        CancellationToken stoppingToken)
    {
        if (availability.Status != UpdateAvailabilityStatus.UpdateAvailable)
        {
            return;
        }

        var notifyOnlySetting = await settingsReader.GetSetting(SettingsConstants.UPDATE_NOTIFY_ONLY);
        if (bool.TryParse(notifyOnlySetting?.Value, out var notifyOnly) && notifyOnly)
        {
            return;
        }

        var startSetting = await settingsReader.GetSetting(SettingsConstants.UPDATE_MAINTENANCE_WINDOW_START);
        var endSetting = await settingsReader.GetSetting(SettingsConstants.UPDATE_MAINTENANCE_WINDOW_END);
        if (!IsWithinMaintenanceWindow(startSetting?.Value, endSetting?.Value, TimeOnly.FromDateTime(DateTime.UtcNow)))
        {
            _logger.LogInformation("Update detection: outside maintenance window, auto-trigger skipped.");
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new TriggerUpdateCommand("auto"), stoppingToken);
        _logger.LogInformation("Update detection: auto-trigger result enqueued={Enqueued} ({Reason}).", result.Enqueued, result.Reason);
    }

    private static bool IsWithinMaintenanceWindow(string? start, string? end, TimeOnly now)
    {
        if (!TimeOnly.TryParse(start, out var from) || !TimeOnly.TryParse(end, out var to))
        {
            return true;
        }

        return from <= to ? now >= from && now <= to : now >= from || now <= to;
    }
}
