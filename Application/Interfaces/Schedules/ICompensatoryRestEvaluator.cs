// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read path for K12 stage-1 compensatory-rest tracking: reports every open (unfulfilled) obligation of
/// one client as a feed entry — "due" while inside its deadline, "overdue" once the deadline has passed —
/// escalating to Error when the compensatoryRest compliance rule is in Block mode. Does not write; the
/// obligations themselves are produced by the reconciler. Real mode only.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface ICompensatoryRestEvaluator
{
    Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
