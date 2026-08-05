// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

/// <summary>
/// One SignalR event for a whole batch of newly created works. Applying a wizard result created one
/// event per work, so a plan with several hundred assignments produced several hundred round trips and
/// as many client-side refresh decisions. The date range spans every affected period so the hub can
/// resolve the target connections once.
/// </summary>
/// <param name="Works">The created works, each in the same shape as the single WorkCreated event.</param>
/// <param name="StartDate">Earliest period start across the batch.</param>
/// <param name="EndDate">Latest period end across the batch.</param>
/// <param name="SourceConnectionId">Connection that caused the change; excluded from the broadcast.</param>
/// <param name="AnalyseToken">Scenario the batch belongs to; null is the main schedule.</param>
public sealed record WorksBulkCreatedNotificationDto(
    IReadOnlyList<WorkNotificationDto> Works,
    DateOnly StartDate,
    DateOnly EndDate,
    string SourceConnectionId,
    Guid? AnalyseToken);
