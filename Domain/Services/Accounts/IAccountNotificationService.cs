namespace Klacks.Api.Domain.Services.Accounts;

public interface IAccountNotificationService
{
    Task<string> SendEmailAsync(string title, string email, string message);
}