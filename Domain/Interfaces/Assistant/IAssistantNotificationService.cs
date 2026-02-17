namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAssistantNotificationService
{
    Task SendProactiveMessageAsync(string userId, string message, string? conversationId = null);

    Task SendOnboardingPromptAsync(string userId, string message);

    bool IsUserConnected(string userId);

    IEnumerable<string> GetConnectedUserIds();
}
