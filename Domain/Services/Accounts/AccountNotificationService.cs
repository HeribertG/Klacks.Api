using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Domain.Services.Accounts;

public class AccountNotificationService : IAccountNotificationService
{
    private readonly DataBaseContext _appDbContext;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly ILogger<AccountNotificationService> _logger;

    public AccountNotificationService(
        DataBaseContext appDbContext,
        ISettingsEncryptionService encryptionService,
        ILogger<AccountNotificationService> logger)
    {
        _appDbContext = appDbContext;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<string> SendEmailAsync(string title, string email, string message)
    {
        if (email == null)
        {
            throw new ArgumentNullException(nameof(email), "Email address cannot be null");
        }
        
        _logger.LogInformation("Attempting to send email to {Email} with title: {Title}", email, title);
        
        try
        {
            if (_appDbContext?.Settings == null)
            {
                _logger.LogWarning("Database or Settings table not available for email sending");
                return "Email configuration not available";
            }

            if (!await CanAccessDatabaseAsync())
            {
                _logger.LogWarning("Cannot access database for email configuration");
                return "Database connection not available";
            }

            var mail = new MsgEMail(_appDbContext, _encryptionService);
            var result = mail.SendMail(email, title, message);
            
            _logger.LogInformation("Email sent successfully to {Email}", email);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}. Returning failure message.", email);
            return $"Email sending failed: {ex.Message}";
        }
    }

    private async Task<bool> CanAccessDatabaseAsync()
    {
        try
        {
            await _appDbContext.Database.CanConnectAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}