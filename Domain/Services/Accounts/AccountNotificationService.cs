// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Domain.Services.Accounts;

public class AccountNotificationService : IAccountNotificationService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountNotificationService> _logger;

    public AccountNotificationService(
        IEmailService emailService,
        ILogger<AccountNotificationService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<string> SendEmailAsync(string title, string email, string message)
    {
        if (email == null)
        {
            throw new ArgumentNullException(nameof(email), "Email address cannot be null");
        }

        _logger.LogInformation("Attempting to send email with title: {Title}", title);

        try
        {
            if (!await _emailService.CanSendEmailAsync())
            {
                _logger.LogWarning("Email service not available");
                return "Email configuration not available";
            }

            var result = _emailService.SendMail(email, title, message);

            _logger.LogInformation("Email sent successfully with title: {Title}", title);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with title: {Title}. Returning failure message.", title);
            return $"Email sending failed: {ex.Message}";
        }
    }
}
