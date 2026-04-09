// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IPluginEmailSender to the host IEmailService.
/// Returns false when the email service is not configured.
/// </summary>
/// <param name="emailService">Host transactional email service.</param>
/// <param name="logger">Logger for diagnostic output.</param>

using Klacks.Api.Domain.Interfaces;
using Klacks.Plugin.Contracts;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Plugins;

public class PluginEmailSenderBridge : IPluginEmailSender
{
    private readonly IEmailService _emailService;
    private readonly ILogger<PluginEmailSenderBridge> _logger;

    public PluginEmailSenderBridge(IEmailService emailService, ILogger<PluginEmailSenderBridge> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toAddress, string subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
            return false;

        if (!await _emailService.CanSendEmailAsync())
        {
            _logger.LogWarning("Plugin email send skipped — email service not available");
            return false;
        }

        try
        {
            _emailService.SendMail(toAddress, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin email send failed for {Recipient}", toAddress);
            return false;
        }
    }
}
