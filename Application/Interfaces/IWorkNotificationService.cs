using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkNotificationService
{
    Task NotifyWorkCreated(WorkNotificationDto notification);
    Task NotifyWorkUpdated(WorkNotificationDto notification);
    Task NotifyWorkDeleted(WorkNotificationDto notification);
    Task NotifyScheduleUpdated(ScheduleNotificationDto notification);
    Task NotifyPeriodHoursUpdated(PeriodHoursNotificationDto notification);
    Task NotifyPeriodHoursRecalculated(DateOnly startDate, DateOnly endDate);
    Task NotifyScheduleChangeTracked(ScheduleChangeNotificationDto notification);
}
