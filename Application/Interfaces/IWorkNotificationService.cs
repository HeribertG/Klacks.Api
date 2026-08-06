// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkNotificationService
{
    Task NotifyWorkCreated(WorkNotificationDto notification);

    /// <summary>
    /// Sends one event for a whole batch of created works instead of one per work.
    /// </summary>
    /// <param name="notification">The batch, carrying the date range spanning every affected period.</param>
    Task NotifyWorksBulkCreated(WorksBulkCreatedNotificationDto notification);

    /// <summary>
    /// Tells the people looking at the real schedule of a group that a background candidate appeared,
    /// was replaced or timed out.
    /// </summary>
    /// <param name="notification">The candidate and what happened to it.</param>
    Task NotifyWizard4CandidatesChanged(Wizard4CandidateNotificationDto notification);
    Task NotifyWorkUpdated(WorkNotificationDto notification);
    Task NotifyWorkDeleted(WorkNotificationDto notification);
    Task NotifyScheduleUpdated(ScheduleNotificationDto notification);
    Task NotifyPeriodHoursUpdated(PeriodHoursNotificationDto notification);
    Task NotifyPeriodHoursRecalculated(DateOnly startDate, DateOnly endDate, Guid? analyseToken);
    Task NotifyThoroughRecalculationCompleted(ThoroughRecalculationCompletedDto notification);
    Task NotifyScheduleChangeTracked(ScheduleChangeNotificationDto notification);
    Task NotifyCollisionsDetected(CollisionListNotificationDto notification);
    Task NotifyScheduleValidationsDetected(ScheduleValidationListNotificationDto notification);
}
