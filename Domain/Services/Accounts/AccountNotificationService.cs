using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Domain.Services.Accounts;

public class AccountNotificationService : IAccountNotificationService
{
    private readonly DataBaseContext _appDbContext;
    private readonly ILogger<AccountNotificationService> _logger;

    public AccountNotificationService(
        DataBaseContext appDbContext,
        ILogger<AccountNotificationService> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
    }

    public Task<string> SendEmailAsync(string title, string email, string message)
    {
        _logger.LogInformation("Sending email to {Email} with title: {Title}", email, title);
        
        try
        {
            var mail = new MsgEMail(_appDbContext);
            var result = mail.SendMail(email, title, message);
            
            _logger.LogInformation("Email sent successfully to {Email}", email);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            throw;
        }
    }
}