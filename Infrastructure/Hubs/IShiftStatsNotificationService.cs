using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IShiftStatsNotificationService
{
    Task NotifyShiftStatsUpdated(ShiftStatsNotificationDto notification);
}
