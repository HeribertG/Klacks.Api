using Klacks.Api.Presentation.DTOs.Notifications;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IWorkNotificationService
{
    Task NotifyWorkCreated(WorkNotificationDto notification);
    Task NotifyWorkUpdated(WorkNotificationDto notification);
    Task NotifyWorkDeleted(WorkNotificationDto notification);
}
