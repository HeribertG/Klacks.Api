using Klacks.Api.Application.Constants;
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

    private const string OnboardingMessage =
        "Hi! I'm your proactive assistant. I can periodically check things for you " +
        "- like shift conflicts, scheduling gaps, or contract expirations. " +
        "Just tell me what you'd like me to monitor, and I'll keep an eye on it for you!";

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
        var notificationService = scope.ServiceProvider.GetRequiredService<IAssistantNotificationService>();

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
                    await HandleOnboardingAsync(userId, repo, notificationService, cancellationToken);
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

    private async Task HandleOnboardingAsync(
        string userId,
        IHeartbeatConfigRepository repo,
        IAssistantNotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var config = new HeartbeatConfig
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = false,
            OnboardingCompleted = false,
            IntervalMinutes = 30,
            ActiveHoursStart = new TimeOnly(8, 0),
            ActiveHoursEnd = new TimeOnly(22, 0),
            ChecklistJson = "[]"
        };

        await repo.AddAsync(config, cancellationToken);
        await notificationService.SendOnboardingPromptAsync(userId, OnboardingMessage);

        _logger.LogInformation("Sent onboarding prompt to user {UserId}", userId);
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

        var message = $"Heartbeat check completed. Checklist: {config.ChecklistJson}";

        await notificationService.SendProactiveMessageAsync(userId, message);

        config.LastExecutedAt = DateTime.UtcNow;
        await repo.UpdateAsync(config, cancellationToken);

        _logger.LogInformation("Executed heartbeat for user {UserId}", userId);
    }

    private static async Task<bool> IsGloballyEnabledAsync(IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        var context = scopedProvider.GetRequiredService<Klacks.Api.Infrastructure.Persistence.DataBaseContext>();
        var setting = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(context.Settings, s => s.Type == Settings.HEARTBEAT_ENABLED_GLOBALLY, cancellationToken);

        return setting?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? true;
    }
}
