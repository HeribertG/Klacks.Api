using Klacks.Api.Presentation.DTOs.Notifications;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IPeriodHoursNotificationService
{
    Task NotifyPeriodHoursUpdated(PeriodHoursNotificationDto notification);
    Task NotifyPeriodHoursRecalculated(DateOnly startDate, DateOnly endDate);
}
