// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs the periodic heartbeat check for every connected user whose configuration is enabled and
/// onboarded. A user without a configuration gets one created on first sight, disabled, so the row
/// exists before ConfigureHeartbeatSkill switches it on — no chat message is sent for it: an
/// unsolicited server-composed notice cannot be localized (the server does not know the connected
/// user's UI language) and was not wanted. IAssistantNotificationService.SendOnboardingPromptAsync
/// therefore has no caller left; it stays as the transport should a localized invitation be introduced.
/// </summary>
/// <param name="serviceProvider">Root provider; each tick resolves its own scope for the repositories.</param>
/// <param name="tracker">Which users currently hold an assistant connection.</param>
/// <param name="logger">Structured log of ticks, onboarding rows created and per-user failures.</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Hubs;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class HeartbeatBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAssistantConnectionTracker _tracker;
    private readonly ILogger<HeartbeatBackgroundService> _logger;
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HeartbeatBackgroundService(
        IServiceProvider serviceProvider,
        IAssistantConnectionTracker tracker,
        ILogger<HeartbeatBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _tracker = tracker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HeartbeatBackgroundService started");

        try
        {
            using var timer = new PeriodicTimer(TickInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessTickAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error during heartbeat tick");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("HeartbeatBackgroundService stopped");
    }

    private async Task ProcessTickAsync(CancellationToken cancellationToken)
    {
        var connectedUserIds = _tracker.GetConnectedUserIds().ToList();
        if (connectedUserIds.Count == 0)
            return;

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IHeartbeatConfigRepository>();

        var globalEnabled = await IsGloballyEnabledAsync(scope.ServiceProvider, cancellationToken);
        if (!globalEnabled)
            return;

        foreach (var userId in connectedUserIds)
        {
            try
            {
                var config = await repo.GetByUserIdAsync(userId, cancellationToken);

                if (config == null)
                {
                    await CreateDisabledConfigAsync(userId, repo, cancellationToken);
                }
                else if (config.IsEnabled && config.OnboardingCompleted && ShouldExecute(config))
                {
                    await ExecuteHeartbeatAsync(userId, config, scope.ServiceProvider, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing heartbeat for user {UserId}", userId);
            }
        }
    }

    private async Task CreateDisabledConfigAsync(
        string userId,
        IHeartbeatConfigRepository repo,
        CancellationToken cancellationToken)
    {
        var config = new HeartbeatConfig
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = false,
            OnboardingCompleted = false,
            IntervalMinutes = HeartbeatDefaults.DefaultIntervalMinutes,
            ActiveHoursStart = HeartbeatDefaults.DefaultActiveHoursStart,
            ActiveHoursEnd = HeartbeatDefaults.DefaultActiveHoursEnd,
            ChecklistJson = HeartbeatDefaults.DefaultChecklistJson
        };

        await repo.AddAsync(config, cancellationToken);

        _logger.LogInformation("Created disabled heartbeat config for user {UserId}", userId);
    }

    private bool ShouldExecute(HeartbeatConfig config)
    {
        var now = DateTime.UtcNow;
        var currentTime = TimeOnly.FromDateTime(now);

        if (currentTime < config.ActiveHoursStart || currentTime > config.ActiveHoursEnd)
            return false;

        if (config.LastExecutedAt == null)
            return true;

        return (now - config.LastExecutedAt.Value).TotalMinutes >= config.IntervalMinutes;
    }

    private async Task ExecuteHeartbeatAsync(
        string userId,
        HeartbeatConfig config,
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        var notificationService = scopedProvider.GetRequiredService<IAssistantNotificationService>();
        var repo = scopedProvider.GetRequiredService<IHeartbeatConfigRepository>();
        var llmService = scopedProvider.GetRequiredService<IHeartbeatLLMService>();
        var dataCollector = scopedProvider.GetRequiredService<IHeartbeatDataCollector>();

        var checkItems = ParseChecklistJson(config.ChecklistJson);
        var since = config.LastExecutedAt ?? DateTime.UtcNow.AddMinutes(-config.IntervalMinutes);

        var snapshot = await dataCollector.CollectAsync(since, cancellationToken);

        var message = await llmService.GenerateStatusMessageAsync(checkItems, snapshot, cancellationToken);

        if (!string.IsNullOrWhiteSpace(message))
        {
            await notificationService.SendProactiveMessageAsync(userId, message);
        }

        config.LastExecutedAt = DateTime.UtcNow;
        await repo.UpdateAsync(config, cancellationToken);

        _logger.LogInformation("Executed heartbeat for user {UserId}", userId);
    }

    private List<HeartbeatCheckItem> ParseChecklistJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<HeartbeatCheckItem>>(json, JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse heartbeat checklist JSON");
            return [];
        }
    }

    private static async Task<bool> IsGloballyEnabledAsync(IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        var settingsReader = scopedProvider.GetRequiredService<Klacks.Api.Domain.Interfaces.Settings.ISettingsReader>();
        var setting = await settingsReader.GetSetting(Application.Constants.Settings.HEARTBEAT_ENABLED_GLOBALLY);
        return setting?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? true;
    }
}
