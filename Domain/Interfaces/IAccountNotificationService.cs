namespace Klacks.Api.Domain.Interfaces;

public interface IAccountNotificationService
{
    Task<string> SendEmailAsync(string title, string email, string message);
}