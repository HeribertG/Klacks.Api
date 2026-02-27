// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;

namespace Klacks.Api.Infrastructure.Email;

public class EmailPollingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailPollingBackgroundService> _logger;

    private const int DefaultIntervalSeconds = 300;

    public EmailPollingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailPollingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email polling background service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var intervalSeconds = DefaultIntervalSeconds;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var settingsRepository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

                    var serverSetting = await settingsRepository.GetSetting(Settings.APP_INCOMING_SERVER);
                    if (string.IsNullOrWhiteSpace(serverSetting?.Value))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
                        continue;
                    }

                    var intervalSetting = await settingsRepository.GetSetting(Settings.APP_INCOMING_SERVER_POLL_INTERVAL);
                    if (int.TryParse(intervalSetting?.Value, out var configuredInterval) && configuredInterval > 0)
                    {
                        intervalSeconds = configuredInterval;
                    }

                    var emailService = scope.ServiceProvider.GetRequiredService<IImapEmailService>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var newEmails = await emailService.FetchNewEmailsAsync(stoppingToken);

                    if (newEmails.Count > 0)
                    {
                        var spamFilterService = scope.ServiceProvider.GetRequiredService<ISpamFilterService>();
                        foreach (var email in newEmails.Where(e => string.Equals(e.Folder, "INBOX", StringComparison.OrdinalIgnoreCase)))
                        {
                            var spamResult = await spamFilterService.ClassifyAsync(email, stoppingToken);
                            if (spamResult.IsSpam)
                            {
                                email.Folder = "Junk";
                                _logger.LogInformation("Email from {From} classified as spam: {Reason}", email.FromAddress, spamResult.Reason);
                            }
                        }

                        await unitOfWork.CompleteAsync();
                        _logger.LogInformation("Saved {Count} new emails to database", newEmails.Count);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in email polling background service");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Email polling background service stopped");
    }
}
