// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background service that enforces message retention policy by deleting old messages.
/// Runs hourly and keeps only the configured number of messages per provider.
/// </summary>
/// <param name="_serviceProvider">Service provider for creating scoped services</param>
/// <param name="_logger">Logger instance</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Messaging;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Messaging;

public class MessageRetentionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageRetentionService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public MessageRetentionService(IServiceProvider serviceProvider, ILogger<MessageRetentionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageRetentionService started");

        try
        {
            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await EnforceRetentionAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error during message retention enforcement");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }

        _logger.LogInformation("MessageRetentionService stopped");
    }

    private async Task EnforceRetentionAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var providerRepo = scope.ServiceProvider.GetRequiredService<IMessagingProviderRepository>();
        var messageRepo = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
        var settingsReader = scope.ServiceProvider.GetRequiredService<ISettingsReader>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var retentionSetting = await settingsReader.GetSetting(MessagingConstants.SettingRetentionCount);
        var retainCount = int.TryParse(retentionSetting?.Value, out var rc) ? rc : MessagingConstants.DefaultRetentionCount;

        var providers = await providerRepo.GetAllAsync();

        foreach (var provider in providers)
        {
            var count = await messageRepo.GetMessageCountAsync(provider.Id);
            if (count > retainCount)
            {
                await messageRepo.DeleteOldestMessagesAsync(provider.Id, retainCount);
                _logger.LogInformation("Retained {RetainCount} messages for provider {Provider}, deleted {Deleted}", retainCount, provider.Name, count - retainCount);
            }
        }

        await unitOfWork.CompleteAsync();
    }
}
