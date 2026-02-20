using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IScheduleClient
{
    Task WorkCreated(WorkNotificationDto notification);
    Task WorkUpdated(WorkNotificationDto notification);
    Task WorkDeleted(WorkNotificationDto notification);
    Task ScheduleUpdated(ScheduleNotificationDto notification);
    Task ShiftStatsUpdated(ShiftStatsNotificationDto notification);
    Task PeriodHoursUpdated(PeriodHoursNotificationDto notification);
    Task PeriodHoursRecalculated(PeriodHoursRecalculatedDto notification);
    Task ScheduleChangeTracked(ScheduleChangeNotificationDto notification);
}
